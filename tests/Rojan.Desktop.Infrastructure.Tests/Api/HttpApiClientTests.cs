using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Api;

namespace Rojan.Desktop.Infrastructure.Tests.Api;

/// <summary>Exercises the parts of <see cref="HttpApiClient"/> that do not require an actual reachable backend (there is none configured - see the class's own doc comment): the connectivity short-circuit and the "no base address configured" guard.</summary>
public sealed class HttpApiClientTests
{
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

    private sealed class StubSessionService : ISessionService
    {
        public SessionIdentity? CurrentSession => null;

        public AuthToken? CurrentAccessToken => null;

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
}
