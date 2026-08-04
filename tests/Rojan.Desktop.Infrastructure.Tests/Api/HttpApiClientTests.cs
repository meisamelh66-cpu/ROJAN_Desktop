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
        using var client = new HttpApiClient(new StubConnectivityService(ConnectionState.Offline), new PassThroughRetryPolicy(), new StubSessionService(), new StubApiEnvironmentService(null));

        await Assert.ThrowsAsync<ApiConnectivityException>(() => client.GetAsync<string>("health"));
    }

    [Fact]
    public async Task GetAsync_OnlineButNoBaseAddressConfigured_ThrowsApiConnectivityException()
    {
        using var client = new HttpApiClient(new StubConnectivityService(ConnectionState.Online), new PassThroughRetryPolicy(), new StubSessionService(), new StubApiEnvironmentService(null));

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
    public async Task PatchAsync_SendsPatchMethodWithNoBodyAndReturnsDeserializedResponse()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, """{"value":"confirmed"}""")));
        using var client = CreateClient(handler);

        var response = await client.PatchAsync<TestResponse>("bookings/1/confirm");

        Assert.True(response.IsSuccess);
        Assert.Equal("confirmed", response.Data?.Value);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest?.Method);
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

    [Fact]
    public async Task GetAsync_ForbiddenStatusCode_ThrowsImmediatelyWithoutAttemptingARefresh()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));
        var session = new StubSessionService();
        using var client = CreateClient(handler, session);

        await Assert.ThrowsAsync<ApiAuthenticationException>(() => client.GetAsync<TestResponse>("items/1"));

        Assert.Equal(0, session.RefreshCallCount);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetAsync_UnauthorizedStatusCode_AttemptsExactlyOneRefresh()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var session = new StubSessionService();
        using var client = CreateClient(handler, session);

        await Assert.ThrowsAsync<ApiAuthenticationException>(() => client.GetAsync<TestResponse>("items/1"));

        Assert.Equal(1, session.RefreshCallCount);
    }

    [Fact]
    public async Task GetAsync_UnauthorizedThenRefreshSucceeds_RetriesOnceWithTheRefreshedTokenAndReturnsSuccess()
    {
        var attempt = 0;
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            attempt++;
            return Task.FromResult(attempt == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : JsonResponse(HttpStatusCode.OK, """{"value":"hello"}"""));
        });
        var refreshedToken = new AuthToken("refreshed-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var session = new StubSessionService(new AuthToken("stale-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)), tokenAfterRefresh: refreshedToken);
        using var client = CreateClient(handler, session);

        var response = await client.GetAsync<TestResponse>("items/1");

        Assert.True(response.IsSuccess);
        Assert.Equal("hello", response.Data?.Value);
        Assert.Equal(1, session.RefreshCallCount);
        Assert.Equal(2, handler.CallCount);
        Assert.Equal("refreshed-token", handler.LastRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetAsync_RefreshAsyncThrows_ThrowsApiAuthenticationExceptionWithoutRetrying()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var session = new StubSessionService(refreshException: new InvalidOperationException("no refresh token"));
        using var client = CreateClient(handler, session);

        var exception = await Assert.ThrowsAsync<ApiAuthenticationException>(() => client.GetAsync<TestResponse>("items/1"));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetAsync_StillUnauthorizedAfterOneRetry_ThrowsWithoutAttemptingASecondRefresh()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var session = new StubSessionService();
        using var client = CreateClient(handler, session);

        await Assert.ThrowsAsync<ApiAuthenticationException>(() => client.GetAsync<TestResponse>("items/1"));

        Assert.Equal(1, session.RefreshCallCount);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetAsync_CallerCancelsWhileTheRetriedRequestAfterARefreshIsInFlight_ThrowsOperationCanceledException()
    {
        var attempt = 0;
        var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            attempt++;
            if (attempt == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var session = new StubSessionService();
        using var client = CreateClient(handler, session);
        using var cts = new CancellationTokenSource();

        var task = client.GetAsync<TestResponse>("items/1", cts.Token);
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
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

    private sealed class StubApiEnvironmentService(Uri? resolvedBaseAddress) : IApiEnvironmentService
    {
        public ApiEnvironment SelectedEnvironment => ApiEnvironment.Development;

        public Uri? ResolvedBaseAddress => resolvedBaseAddress;

        public string? ProductionUrl => null;

        public bool IsRestartRequired => false;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetEnvironmentAsync(ApiEnvironment environment, string? productionUrl, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");
    }

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

    /// <summary>
    /// Sprint 7 Commit 2: <see cref="RefreshAsync"/> now has to be
    /// exercisable, not just a placeholder that throws - a real 401 flows
    /// through <see cref="HttpApiClient"/>'s new refresh-and-retry-once
    /// path, which calls it. By default refresh "succeeds" without
    /// changing the token (enough for tests that only care about *whether*
    /// a refresh was attempted); <paramref name="tokenAfterRefresh"/> and
    /// <paramref name="refreshException"/> let a test drive the two other
    /// outcomes that matter here (refresh rotates the token / refresh
    /// itself fails).
    /// </summary>
    private sealed class StubSessionService(
        AuthToken? accessToken = null,
        AuthToken? tokenAfterRefresh = null,
        Exception? refreshException = null) : ISessionService
    {
        private AuthToken? _accessToken = accessToken;

        public int RefreshCallCount { get; private set; }

        public SessionIdentity? CurrentSession => null;

        public AuthToken? CurrentAccessToken => _accessToken;

        public AuthenticationState CurrentState => AuthenticationState.SignedOut;

        public event EventHandler<AuthenticationState>? StateChanged
        {
            add { }
            remove { }
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SessionIdentity> CreateSessionAsync(UserIdentity user, DeviceIdentity device, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<SessionIdentity> CreateSessionFromTokensAsync(
            UserIdentity user,
            DeviceIdentity device,
            AuthToken accessToken,
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<SessionIdentity> RefreshAsync(CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;

            if (refreshException is not null)
            {
                throw refreshException;
            }

            if (tokenAfterRefresh is not null)
            {
                _accessToken = tokenAfterRefresh;
            }

            return Task.FromResult(new SessionIdentity("session-1", "user-1", "device-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)));
        }

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

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return await _responder(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
