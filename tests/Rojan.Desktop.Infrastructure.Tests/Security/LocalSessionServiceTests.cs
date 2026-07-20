using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

/// <summary>Exercises <see cref="LocalSessionService"/> against a temp file - real random token generation and real expiry-timestamp comparisons, never a mocked clock (all assertions treat "now" loosely, e.g. "not yet expired" rather than an exact instant).</summary>
public sealed class LocalSessionServiceTests : IDisposable
{
    private static readonly UserIdentity User = new("user-1", "Test User", "test@example.com");
    private static readonly DeviceIdentity Device = new("device-1", "fingerprint", "MACHINE", "Windows", DateTimeOffset.UtcNow);

    private readonly string _filePath;

    public LocalSessionServiceTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "auth-session.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InitializeAsync_NoPersistedSession_StaysSignedOut()
    {
        var service = new LocalSessionService(_filePath);

        await service.InitializeAsync();

        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
        Assert.Null(service.CurrentSession);
    }

    [Fact]
    public async Task CreateSessionAsync_IssuesAnAuthenticatedSessionWithATokenPair()
    {
        var service = new LocalSessionService(_filePath);

        var session = await service.CreateSessionAsync(User, Device);

        Assert.Equal(User.Id, session.UserId);
        Assert.Equal(Device.Id, session.DeviceId);
        Assert.Equal(AuthenticationState.Authenticated, service.CurrentState);
        Assert.NotNull(service.CurrentAccessToken);
        Assert.False(service.CurrentAccessToken!.IsExpired(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task CreateSessionAsync_PersistsTheSessionForRestoration()
    {
        var first = new LocalSessionService(_filePath);
        var created = await first.CreateSessionAsync(User, Device);

        var second = new LocalSessionService(_filePath);
        await second.InitializeAsync();

        Assert.Equal(created.Id, second.CurrentSession?.Id);
        Assert.Equal(AuthenticationState.Authenticated, second.CurrentState);
    }

    [Fact]
    public async Task RefreshAsync_ExtendsTheSessionAndIssuesANewAccessToken()
    {
        var service = new LocalSessionService(_filePath);
        await service.CreateSessionAsync(User, Device);
        var originalAccessToken = service.CurrentAccessToken!.Value;
        var originalExpiry = service.CurrentSession!.ExpiresAt;

        await service.RefreshAsync();

        Assert.NotEqual(originalAccessToken, service.CurrentAccessToken!.Value);
        Assert.True(service.CurrentSession!.ExpiresAt >= originalExpiry);
        Assert.Equal(AuthenticationState.Authenticated, service.CurrentState);
    }

    [Fact]
    public async Task RefreshAsync_NoCurrentSession_ThrowsInvalidOperationException()
    {
        var service = new LocalSessionService(_filePath);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshAsync());
    }

    [Fact]
    public async Task ExpireAsync_ClearsTheSessionAndDeletesThePersistedFile()
    {
        var service = new LocalSessionService(_filePath);
        await service.CreateSessionAsync(User, Device);

        await service.ExpireAsync();

        Assert.Null(service.CurrentSession);
        Assert.Null(service.CurrentAccessToken);
        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
        Assert.False(File.Exists(_filePath));
    }

    [Fact]
    public async Task StateChanged_RaisedWhenSigningIn()
    {
        var service = new LocalSessionService(_filePath);
        var raisedStates = new List<AuthenticationState>();
        service.StateChanged += (_, state) => raisedStates.Add(state);

        await service.CreateSessionAsync(User, Device);

        Assert.Contains(AuthenticationState.Authenticated, raisedStates);
    }
}
