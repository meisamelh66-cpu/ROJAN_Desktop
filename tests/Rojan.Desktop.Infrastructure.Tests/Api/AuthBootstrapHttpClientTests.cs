using System.Net;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Infrastructure.Api;

namespace Rojan.Desktop.Infrastructure.Tests.Api;

/// <summary>
/// Exercises <see cref="AuthBootstrapHttpClient"/> - the standalone
/// transport used only for login/refresh (see its own doc comment for why
/// it deliberately does not share <see cref="HttpApiClient"/>'s pipeline).
/// Uses the same <see cref="FakeHttpMessageHandler"/>-over-internal-
/// constructor seam <see cref="HttpApiClientTests"/> already established.
/// </summary>
public sealed class AuthBootstrapHttpClientTests
{
    private static readonly Uri TestBaseAddress = new("https://api.rojan.test/");

    [Fact]
    public async Task PostAsync_NoBaseAddressConfigured_ThrowsApiConnectivityExceptionWithoutAttemptingARequest()
    {
        using var client = new AuthBootstrapHttpClient(new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("Should not be called.")), null);

        await Assert.ThrowsAsync<ApiConnectivityException>(() => client.PostAsync<TestRequest, TestResponse>("auth/login", new TestRequest("a")));
    }

    [Fact]
    public async Task PostAsync_SuccessResponse_SerializesRequestAndDeserializesResponseAsCamelCaseJson()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, """{"value":"hello"}""")));
        using var client = new AuthBootstrapHttpClient(handler, TestBaseAddress);

        var result = await client.PostAsync<TestRequest, TestResponse>("auth/login", new TestRequest("Alice"));

        Assert.Equal("hello", result.Value);
        Assert.Equal("""{"name":"Alice"}""", handler.LastRequestBody);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
    }

    [Fact]
    public async Task PostAsync_UnauthorizedResponse_ThrowsApiAuthenticationException()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("Invalid email or password") }));
        using var client = new AuthBootstrapHttpClient(handler, TestBaseAddress);

        var exception = await Assert.ThrowsAsync<ApiAuthenticationException>(() => client.PostAsync<TestRequest, TestResponse>("auth/login", new TestRequest("a")));
        Assert.Contains("401", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostAsync_ForbiddenResponse_ThrowsApiAuthenticationException()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));
        using var client = new AuthBootstrapHttpClient(handler, TestBaseAddress);

        await Assert.ThrowsAsync<ApiAuthenticationException>(() => client.PostAsync<TestRequest, TestResponse>("auth/refresh", new TestRequest("a")));
    }

    [Fact]
    public async Task PostAsync_OtherNonSuccessStatusCode_ThrowsApiException()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Malformed request") }));
        using var client = new AuthBootstrapHttpClient(handler, TestBaseAddress);

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.PostAsync<TestRequest, TestResponse>("auth/login", new TestRequest("a")));
        Assert.IsNotType<ApiAuthenticationException>(exception);
    }

    [Fact]
    public async Task PostAsync_TransportFailure_ThrowsApiConnectivityException()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("DNS failure"));
        using var client = new AuthBootstrapHttpClient(handler, TestBaseAddress);

        await Assert.ThrowsAsync<ApiConnectivityException>(() => client.PostAsync<TestRequest, TestResponse>("auth/login", new TestRequest("a")));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    private sealed record TestRequest(string Name);

    private sealed record TestResponse(string Value);

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return await responder(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
