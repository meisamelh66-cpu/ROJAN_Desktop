using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Secure Authentication Foundation. The sign-in/sign-out
/// workflow, built on top of <see cref="ISessionService"/> - this
/// interface is what a future login screen's ViewModel would depend on,
/// while <see cref="ISessionService"/> stays the lower-level session-
/// lifecycle primitive other services (e.g. the API client attaching an
/// auth header) depend on directly. No credential/password model exists
/// yet (see <see cref="UserIdentity.LocalUser"/>'s own doc comment) - MFA
/// compatibility (Phase 25.3) means <see cref="SignInAsync"/> takes a
/// <see cref="UserIdentity"/> rather than a bare username/password pair,
/// so a second factor can be layered in front of this call without
/// changing its shape.
/// </summary>
public interface IAuthenticationService
{
    public AuthenticationState CurrentState { get; }

    public SessionIdentity? CurrentSession { get; }

    /// <summary>Establishes a new session for <paramref name="user"/> on this device. Idempotent per user - calling again while already signed in as the same user simply refreshes the session.</summary>
    public Task<SessionIdentity> SignInAsync(UserIdentity user, CancellationToken cancellationToken = default);

    public Task SignOutAsync(CancellationToken cancellationToken = default);

    public event EventHandler<AuthenticationState>? StateChanged;
}
