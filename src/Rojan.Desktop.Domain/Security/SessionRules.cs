using Rojan.Desktop.Domain.Identity;

namespace Rojan.Desktop.Domain.Security;

/// <summary>Phase 25: Secure Authentication Foundation. Pure derivation of <see cref="AuthenticationState"/> from a <see cref="SessionIdentity"/> - the single place "is this session usable right now" is decided, so nothing else re-implements the expiry comparison.</summary>
public static class SessionRules
{
    /// <summary>A session within this window of its expiry is still <see cref="AuthenticationState.Authenticated"/> but callers may want to proactively refresh - mirrors <see cref="CertificateRules"/>'s "expiring soon" window.</summary>
    public static readonly TimeSpan ExpiringSoonWindow = TimeSpan.FromMinutes(5);

    public static AuthenticationState DetermineState(SessionIdentity? session, DateTimeOffset now)
    {
        if (session is null)
        {
            return AuthenticationState.SignedOut;
        }

        return now >= session.ExpiresAt ? AuthenticationState.Expired : AuthenticationState.Authenticated;
    }

    /// <summary>True once inside <see cref="ExpiringSoonWindow"/> of <see cref="SessionIdentity.ExpiresAt"/> but not yet expired - the signal a caller uses to refresh proactively rather than waiting for a hard expiry.</summary>
    public static bool IsExpiringSoon(SessionIdentity session, DateTimeOffset now) =>
        now < session.ExpiresAt && session.ExpiresAt - now <= ExpiringSoonWindow;
}
