using System.Net;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Api;

namespace Rojan.Desktop.Infrastructure.Tests.Api;

/// <summary>
/// Exercises <see cref="HttpApiClient"/>. The connectivity-offline and
/// no-base-address tests below predate Sprint 7 Commit 1 and still use
/// the public constructor - there is genuinely no backend to reach, so
/// those two short-circuits are the only behavior testable without a
/// transport seam. Every other test uses the Sprint 7 Commit 1
/// internal constructor overload plus <see cref="FakeHttpMessageHandler"/>
/// to exercise the full request/response pipeline (PUT/DELETE, JSON
/// serialization, non-2xx/401 handling, Bearer attachment, cancellation)
/// without any real network call.
/// </summary>
public sealed class HttpApiClientTests
{
    private static readonly Uri TestBaseAddress = new("https://api.rojan.test/");

    [Fact]
    public async Task GetAsync_ConnectivityIsOffline_ThrowsApiConnectivityExceptionWithoutAttemptingARequest()
    {
        using var client = new HttpApiClient(new StubConnectivityService(ConnectionState.Offline), new PassThroughRetryPolicy(), new StubSessionService());

        await Assert.ThrowsAsync<ApiConnectivityException>(() => client.GetAsync<string>("health"));
    }

    [Fact]
    public async Task GetAsync_OnlineButNoBaseAddressConfigured_ThrowsApiConnectivityException()
    {
        // ROJAN_API_BASE_URL is intentionally unset in this test environment -
        // Phase 25 ships no backend to point it at yet.
        using var client = new HttpApiClient(new StubConnectivityService(ConnectionState.Online), new PassThroughRetryPolicy(), new StubSessionService());

        await Assert.ThrowsAsync<ApiConnectivityException>(() => client.GetAsync<string>("health"));
    }

    [Fact]
    public async Task PutAsync_SendsPutMethodWithSerializedBodyAndReturnsDeserializedResponse()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, """{"value":"updated"}""")));
        using var client = CreateClient(handler);

        var response = await client.PutAsync<TestRequest, TestResponse>("items/1", new TestRequest("new-name"));

        Assert.True(response.IsSuccess);
        Assert.Equal("updated", response.Data?.Value);
        Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
        Assert.Equal("items/1", handler.LastRequest?.RequestUri?.AbsolutePath.TrimStart('/'));
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteMethodWithNoBodyAndReturnsSuccess()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var client = CreateClient(handler);

        var response = await client.DeleteAsync<TestResponse>("items/1");

        Assert.True(response.IsSuccess);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest?.Method);
        Assert.Null(handler.LastRequest?.Content);
    }

    [Fact]
    public async Task PostAsync_SerializesTheRequestBodyAsCamelCaseJson()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, "null")));
        using var client = CreateClient(handler);

        await client.PostAsync<TestRequest, TestResponse>("items", new TestRequest("Alice"));

        Assert.Equal("""{"name":"Alice"}""", handler.LastRequestBody);
        Assert.Equal("application/json", handler.LastRequest?.Content?.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAsync_SuccessResponseBody_DeserializesFromCamelCaseJson()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, """{"value":"hello"}""")));
        using var client = CreateClient(handler);

        var response = await client.GetAsync<TestResponse>("items/1");

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("hello", response.Data?.Value);
    }

    [Fact]
    public async Task GetAsync_NonSuccessStatusCode_ReturnsFailureResponseRatherThanThrowing()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid request") }));
        using var client = CreateClient(handler);

        var response = await client.GetAsync<TestResponse>("items/1");

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal("Invalid request", response.ErrorMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetAsync_AuthenticationFailureStatusCode_ThrowsApiAuthenticationException(HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)));
        using var client = CreateClient(handler);

        await Assert.ThrowsAsync<ApiAuthenticationException>(() => client.GetAsync<TestResponse>("items/1"));
    }

    [Fact]
    public async Task GetAsync_SessionHasAnUnexpiredAccessToken_AttachesBearerAuthorizationHeader()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, "null")));
        var token = new AuthToken("token-value", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5));
        using var client = CreateClient(handler, new StubSessionService(token));

        await client.GetAsync<TestResponse>("items/1");

        Assert.Equal("Bearer", handler.LastRequest?.Headers.Authorization?.Scheme);
        Assert.Equal("token-value", handler.LastRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetAsync_SessionHasAnExpiredAccessToken_DoesNotAttachAuthorizationHeader()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, "null")));
        var expiredToken = new AuthToken("token-value", DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddMinutes(-1));
        using var client = CreateClient(handler, new StubSessionService(expiredToken));

        await client.GetAsync<TestResponse>("items/1");

        Assert.Null(handler.LastRequest?.Headers.Authorization);
    }

    [Fact]
    public async Task GetAsync_CallerCancelsBeforeResponseArrives_ThrowsOperationCanceledExceptionNotApiException()
    {
        var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = CreateClient(handler);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync<TestResponse>("items/1", cts.Token));
    }

    private static HttpApiClient CreateClient(HttpMessageHandler handler, ISessionService? sessionService = null) =>
        new(new StubConnectivityService(ConnectionState.Online), new PassThroughRetryPolicy(), sessionService ?? new StubSessionService(), handler, TestBaseAddress);

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    private sealed record TestRequest(string Name);

    private sealed record TestResponse(string Value);

    private sealed class StubConnectivityService(ConnectionState state) : IConnectivityService
    {
        public ConnectionState CurrentState { get; } = state;

        public event EventHandler<ConnectionState>? StateChanged
        {
            add { }
            remove { }
        }

        public Task<ConnectionState> CheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(CurrentState);

        public void Dispose()
        {
        }
    }

    private sealed class StubSessionService(AuthToken? accessToken = null) : ISessionService
    {
        public SessionIdentity? CurrentSession => null;

        public AuthToken? CurrentAccessToken => accessToken;

        public AuthenticationState CurrentState => AuthenticationState.SignedOut;

        public event EventHandler<AuthenticationState>? StateChanged
        {
            add { }
            remove { }
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SessionIdentity> CreateSessionAsync(UserIdentity user, DeviceIdentity device, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<SessionIdentity> RefreshAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task ExpireAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PassThroughRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    /// <summary>
    /// Sprint 7 Commit 1: a minimal fake transport - captures the last
    /// outgoing request (and its body, read once up front) and answers it
    /// via a caller-supplied responder, so every test above exercises
    /// <see cref="HttpApiClient"/>'s real request/response pipeline without
    /// any real network call.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return await _responder(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
