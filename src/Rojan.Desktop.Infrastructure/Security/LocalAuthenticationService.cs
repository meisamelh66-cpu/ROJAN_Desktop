using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Default <see cref="IAuthenticationService"/>. A thin sign-in/sign-out
/// workflow on top of <see cref="ISessionService"/> - see
/// <see cref="IAuthenticationService"/>'s own doc comment for why the two
/// interfaces stay separate. Ensures the device is registered
/// (<see cref="IDeviceRegistrationService"/>) before creating a session,
/// since a <see cref="SessionIdentity"/> is meaningless without a
/// <see cref="DeviceIdentity"/> to bind it to.
/// </summary>
public sealed class LocalAuthenticationService : IAuthenticationService, IDisposable
{
    private readonly ISessionService _sessionService;
    private readonly IDeviceRegistrationService _deviceRegistrationService;

    public LocalAuthenticationService(ISessionService sessionService, IDeviceRegistrationService deviceRegistrationService)
    {
        _sessionService = sessionService;
        _deviceRegistrationService = deviceRegistrationService;
        _sessionService.StateChanged += OnSessionStateChanged;
    }

    public AuthenticationState CurrentState => _sessionService.CurrentState;

    public SessionIdentity? CurrentSession => _sessionService.CurrentSession;

    public event EventHandler<AuthenticationState>? StateChanged;

    public async Task<SessionIdentity> SignInAsync(UserIdentity user, CancellationToken cancellationToken = default)
    {
        var device = _deviceRegistrationService.CurrentDevice
            ?? await _deviceRegistrationService.EnsureRegisteredAsync(cancellationToken).ConfigureAwait(false);

        return await _sessionService.CreateSessionAsync(user, device, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>This local-only implementation has no backend to authenticate credentials against - see <see cref="BackendAuthenticationService"/> for the real implementation.</summary>
    public Task SignInWithCredentialsAsync(string email, string password, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(LocalAuthenticationService)} has no backend to authenticate credentials against - use {nameof(BackendAuthenticationService)}.");

    /// <summary>This local-only implementation has no backend to request an OTP code from - see <see cref="BackendAuthenticationService"/> for the real implementation.</summary>
    public Task<OtpChallenge> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(LocalAuthenticationService)} has no backend to request an OTP code from - use {nameof(BackendAuthenticationService)}.");

    /// <summary>This local-only implementation has no backend to verify an OTP code against - see <see cref="BackendAuthenticationService"/> for the real implementation.</summary>
    public Task SignInWithOtpAsync(string phoneNumber, string code, string? fullName = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(LocalAuthenticationService)} has no backend to verify an OTP code against - use {nameof(BackendAuthenticationService)}.");

    public Task SignOutAsync(CancellationToken cancellationToken = default) =>
        _sessionService.ExpireAsync(cancellationToken);

    public void Dispose() => _sessionService.StateChanged -= OnSessionStateChanged;

    private void OnSessionStateChanged(object? sender, AuthenticationState state) => StateChanged?.Invoke(this, state);
}
