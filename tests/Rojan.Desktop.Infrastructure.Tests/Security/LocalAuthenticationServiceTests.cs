using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Identity;
using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

/// <summary>Backed by real <see cref="LocalSessionService"/>/<see cref="DeviceRegistrationService"/> instances over temp files - exercises the full sign-in/sign-out workflow, not mocked collaborators.</summary>
public sealed class LocalAuthenticationServiceTests : IDisposable
{
    private static readonly UserIdentity User = new("user-1", "Test User", "test@example.com");

    private readonly string _root;

    public LocalAuthenticationServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private LocalAuthenticationService CreateService() =>
        new(
            new LocalSessionService(Path.Combine(_root, "auth-session.json")),
            new DeviceRegistrationService(Path.Combine(_root, "device.json")));

    [Fact]
    public void CurrentState_BeforeAnySignIn_IsSignedOut()
    {
        using var service = CreateService();

        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
    }

    [Fact]
    public async Task SignInAsync_RegistersTheDeviceAndCreatesAnAuthenticatedSession()
    {
        using var service = CreateService();

        var session = await service.SignInAsync(User);

        Assert.Equal(User.Id, session.UserId);
        Assert.Equal(AuthenticationState.Authenticated, service.CurrentState);
        Assert.NotNull(service.CurrentSession);
    }

    [Fact]
    public async Task SignOutAsync_ClearsTheSession()
    {
        using var service = CreateService();
        await service.SignInAsync(User);

        await service.SignOutAsync();

        Assert.Equal(AuthenticationState.SignedOut, service.CurrentState);
        Assert.Null(service.CurrentSession);
    }

    [Fact]
    public async Task StateChanged_RelaysTheUnderlyingSessionServicesEvent()
    {
        using var service = CreateService();
        var raised = new List<AuthenticationState>();
        service.StateChanged += (_, state) => raised.Add(state);

        await service.SignInAsync(User);
        await service.SignOutAsync();

        Assert.Contains(AuthenticationState.Authenticated, raised);
        Assert.Contains(AuthenticationState.SignedOut, raised);
    }
}
