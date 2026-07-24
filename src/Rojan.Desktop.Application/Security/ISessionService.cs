using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Secure Authentication Foundation. Owns the current
/// <see cref="SessionIdentity"/>'s lifecycle (create, persist, restore,
/// refresh, expire) - the layer <see cref="IAuthenticationService"/> sits
/// on top of. Kept as its own interface (rather than folded into
/// <see cref="IAuthenticationService"/>) because session persistence/
/// restoration across app restarts is a distinct concern from the
/// login/logout workflow, mirroring how <c>ICurrentSessionService</c> and
/// the future login UI are already expected to be separate concerns.
/// </summary>
public interface ISessionService
{
    /// <summary>Null when signed out or after <see cref="ExpireAsync"/>.</summary>
    public SessionIdentity? CurrentSession { get; }

    public AuthToken? CurrentAccessToken { get; }

    public AuthenticationState CurrentState { get; }

    /// <summary>Restores a previously-persisted session (if any and not expired) - called once at startup, same shape as <c>ICurrentSessionService.InitializeAsync</c>.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Issues and persists a new session for <paramref name="user"/> on <paramref name="device"/>, replacing any current session.</summary>
    public Task<SessionIdentity> CreateSessionAsync(UserIdentity user, DeviceIdentity device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a new <see cref="AuthToken"/>/<see cref="RefreshToken"/> pair
    /// for <see cref="CurrentSession"/>, extending its
    /// <see cref="SessionIdentity.ExpiresAt"/>. Throws
    /// <see cref="InvalidOperationException"/> if there is no current
    /// session.
    ///
    /// Sprint 7 Commit 5: today this rotation happens entirely locally
    /// (no backend call - see the concrete implementation's own doc
    /// comment). <see cref="Api.Contracts.AuthRefreshRequest"/>/
    /// <see cref="Api.Contracts.AuthRefreshResponse"/> define the wire
    /// shape a future backend-backed implementation of this same method
    /// would send/receive - this signature does not change to accommodate
    /// that; only what happens inside a future implementation would.
    /// </summary>
    public Task<SessionIdentity> RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the current session and its persisted state.</summary>
    public Task ExpireAsync(CancellationToken cancellationToken = default);

    public event EventHandler<AuthenticationState>? StateChanged;
}
