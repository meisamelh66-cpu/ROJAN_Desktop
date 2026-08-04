using Rojan.Desktop.Application.Api;
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

    /// <summary>
    /// Owner App Backend Integration: authenticates against the real
    /// backend's <c>POST /auth/login</c> with <paramref name="email"/>/
    /// <paramref name="password"/> and establishes a session from the
    /// returned token pair. Distinct from <see cref="SignInAsync"/> (which
    /// takes an already-resolved <see cref="UserIdentity"/> and has no
    /// credential concept) because this is the only entry point that can
    /// actually reach the backend - the <see cref="UserIdentity"/> for the
    /// new session is built from the login response's own user object, not
    /// supplied by the caller. Throws <see cref="ApiAuthenticationException"/>
    /// on invalid credentials, <see cref="ApiConnectivityException"/> if
    /// the backend is unreachable. Returns plain <see cref="Task"/> rather
    /// than <see cref="SessionIdentity"/> (unlike <see cref="SignInAsync"/>) -
    /// this is the one <c>IAuthenticationService</c> member a Presentation-
    /// layer ViewModel is actually expected to call (see
    /// <c>Rojan.Desktop.ArchitectureTests.DependencyDirectionTests.Presentation_ShouldNotDependOnDomainInfrastructureOrShell</c>),
    /// so its signature must never force that layer to reference a Domain
    /// type; success/failure (exception) is everything a login screen
    /// needs to know.
    /// </summary>
    public Task SignInWithCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Owner App Mobile Login: requests an OTP code for <paramref name="phoneNumber"/>
    /// (E.164, e.g. <c>+989123456789</c>) via the real backend's
    /// <c>POST /auth/otp/request</c> - the first step of the Mobile-First
    /// flow (Mobile Number -&#8594; OTP -&#8594; JWT -&#8594; Secure Storage -&#8594; Dashboard),
    /// establishing no session by itself. Throws <see cref="ApiConnectivityException"/>
    /// if the backend is unreachable, <see cref="ApiException"/> if the
    /// request was rejected (e.g. rate-limited). Returns <see cref="OtpChallenge"/>
    /// (not a Domain type - see that record's own doc comment) rather than
    /// <see langword="void"/> so the caller knows how long to wait before
    /// offering Resend.
    /// </summary>
    public Task<OtpChallenge> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Owner App Mobile Login: verifies <paramref name="code"/> for
    /// <paramref name="phoneNumber"/> via the real backend's
    /// <c>POST /auth/otp/verify</c> and establishes a session from the
    /// returned token pair - the OTP counterpart to <see cref="SignInWithCredentialsAsync"/>,
    /// same reasoning for why this returns plain <see cref="Task"/> rather
    /// than a Domain type. <paramref name="fullName"/> is only used by the
    /// backend the first time this phone number completes verification
    /// (new-account creation); ignored for an existing account. Throws
    /// <see cref="ApiAuthenticationException"/> for an invalid or expired
    /// code, <see cref="ApiConnectivityException"/> if the backend is
    /// unreachable.
    /// </summary>
    public Task SignInWithOtpAsync(string phoneNumber, string code, string? fullName = null, CancellationToken cancellationToken = default);

    public Task SignOutAsync(CancellationToken cancellationToken = default);

    public event EventHandler<AuthenticationState>? StateChanged;
}
