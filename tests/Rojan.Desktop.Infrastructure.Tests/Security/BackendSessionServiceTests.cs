using System.Net;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Api;
using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

/// <summary>
/// Exercises <see cref="BackendSessionService"/> - real backend-issued
/// tokens (never locally generated), persisted via an in-memory
/// <see cref="ISecureStorageService"/> stub (the real
/// <see cref="DpapiSecureStorageService"/> is exercised separately - this
/// suite is about <see cref="BackendSessionService"/>'s own logic, not
/// DPAPI itself).
/// </summary>
public sealed class BackendSessionServiceTests
{
    private static readonly UserIdentity User = new("user-1", "Test User", "test@example.com");
    private static readonly DeviceIdentity Device = new("device-1", "fingerprint", "MACHINE", "Windows", DateTimeOffset.UtcNow);
    private static readonly Uri TestBaseAddress = new("https://api.rojan.test/");

    [Fact]
    public async Task InitializeAsync_NoPersistedSession_StaysSignedOut()
    {
        var service = CreateService(out _, out _);

        await service.InitializeAsync();

        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
        Assert.Null(service.CurrentSession);
    }

    [Fact]
    public async Task CreateSessionFromTokensAsync_PersistsTheBackendIssuedTokensSecurely()
    {
        var service = CreateService(out var storage, out _);
        var accessToken = new AuthToken("real-access-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15));
        var refreshToken = new RefreshToken("real-refresh-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

        var session = await service.CreateSessionFromTokensAsync(User, Device, accessToken, refreshToken);

        Assert.Equal(User.Id, session.UserId);
        Assert.Equal(Device.Id, session.DeviceId);
        Assert.Equal(refreshToken.ExpiresAt, session.ExpiresAt);
        Assert.Equal(AuthenticationState.Authenticated, service.CurrentState);
        Assert.Equal("real-access-token", service.CurrentAccessToken?.Value);
        // What matters here is that persistence went through ISecureStorageService.SetAsync
        // (which the real DpapiSecureStorageService encrypts at rest) rather than a raw
        // File.WriteAllText - this stub doesn't simulate encryption itself, that's DpapiSecureStorageService's
        // own, separately-tested responsibility.
        Assert.True(storage.ContainsKey("auth:session"));
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsNotSupported_SinceABackendSessionNeverGeneratesLocalTokens()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<NotSupportedException>(() => service.CreateSessionAsync(User, Device));
    }

    [Fact]
    public async Task InitializeAsync_RestoresAPreviouslyPersistedSessionFromSecureStorage()
    {
        var first = CreateService(out var storage, out _);
        var accessToken = new AuthToken("access-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15));
        var refreshToken = new RefreshToken("refresh-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));
        var created = await first.CreateSessionFromTokensAsync(User, Device, accessToken, refreshToken);

        var second = new BackendSessionService(new AuthBootstrapHttpClient(new NeverCalledHandler(), TestBaseAddress), storage);
        await second.InitializeAsync();

        Assert.Equal(created.Id, second.CurrentSession?.Id);
        Assert.Equal(AuthenticationState.Authenticated, second.CurrentState);
    }

    [Fact]
    public async Task RefreshAsync_NoCurrentSession_ThrowsInvalidOperationException()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshAsync());
    }

    [Fact]
    public async Task RefreshAsync_CallsTheRealBackendRefreshEndpointAndRotatesTheTokenPair()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/api/v1/auth/refresh", request.RequestUri?.AbsolutePath);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """
                {"accessToken":"new-access","accessTokenExpiresAt":"2026-09-01T00:00:00Z","refreshToken":"new-refresh","refreshTokenExpiresAt":"2026-10-01T00:00:00Z"}
                """));
        });
        var service = new BackendSessionService(new AuthBootstrapHttpClient(handler, TestBaseAddress), new StubSecureStorageService());
        await service.CreateSessionFromTokensAsync(
            User, Device,
            new AuthToken("old-access", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15)),
            new RefreshToken("old-refresh", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));

        var renewed = await service.RefreshAsync();

        Assert.Equal("new-access", service.CurrentAccessToken?.Value);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), renewed.ExpiresAt);
    }

    [Fact]
    public async Task ExpireAsync_ClearsTheSessionAndRemovesItFromSecureStorage()
    {
        var service = CreateService(out var storage, out _);
        await service.CreateSessionFromTokensAsync(
            User, Device,
            new AuthToken("a", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15)),
            new RefreshToken("r", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));

        await service.ExpireAsync();

        Assert.Null(service.CurrentSession);
        Assert.Null(service.CurrentAccessToken);
        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
        Assert.False(storage.ContainsKey("auth:session"));
    }

    private static BackendSessionService CreateService(out StubSecureStorageService storage, out AuthBootstrapHttpClient authClient)
    {
        storage = new StubSecureStorageService();
        authClient = new AuthBootstrapHttpClient(new NeverCalledHandler(), TestBaseAddress);
        return new BackendSessionService(authClient, storage);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    private sealed class NeverCalledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("This test's session service should never call the network.");
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request, cancellationToken);
    }

    /// <summary>In-memory stand-in for <see cref="ISecureStorageService"/> - exposes the raw stored blob via the indexer so tests can assert persistence happened without needing the real DPAPI implementation.</summary>
    internal sealed class StubSecureStorageService : ISecureStorageService
    {
        private readonly Dictionary<string, string> _store = [];

        public string this[string key] => _store[key];

        public bool ContainsKey(string key) => _store.ContainsKey(key);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.GetValueOrDefault(key));

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }
}
