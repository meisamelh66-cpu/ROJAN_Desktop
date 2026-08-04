using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Presentation.Tests.Settings;

/// <summary>Fakes <see cref="IAuthenticationService"/> for SettingsPageViewModel tests - only what its Account section (SignOutCommand) needs.</summary>
internal sealed class StubAuthenticationService : IAuthenticationService
{
    public int SignOutCallCount { get; private set; }

    public AuthenticationState CurrentState { get; private set; } = AuthenticationState.Authenticated;

    public SessionIdentity? CurrentSession => null;

    public event EventHandler<AuthenticationState>? StateChanged;

    public Task<SessionIdentity> SignInAsync(UserIdentity user, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task SignInWithCredentialsAsync(string email, string password, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task<OtpChallenge> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task SignInWithOtpAsync(string phoneNumber, string code, string? fullName = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        SignOutCallCount++;
        CurrentState = AuthenticationState.SignedOut;
        StateChanged?.Invoke(this, CurrentState);
        return Task.CompletedTask;
    }
}
