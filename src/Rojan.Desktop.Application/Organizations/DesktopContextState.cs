namespace Rojan.Desktop.Application.Organizations;

/// <summary>
/// Phase 2B Context State Hardening: which kind of session
/// <c>Presentation.Organizations.ICurrentSessionService</c> has
/// resolved - replaces the single <c>HasRealMembership</c> boolean, which
/// could say a session was "not real" but never *why* (no salon yet, an
/// existing customer with no salon relationship, or an intentionally-
/// demoed session all looked identical). A routing tag only - classifies
/// data the backend (or an explicit local developer choice, see
/// <see cref="IDemoModeProvider"/>) already produced; never itself a
/// permission or role calculation, and never consulted by
/// <see cref="IPermissionEngine"/>/<c>RolePermissions</c>, both unchanged
/// by this phase. See ROJAN_Phase2B_Context_State_Hardening_Plan_v1.md.
/// </summary>
public enum DesktopContextState
{
    /// <summary>Real backend ownership - the resolved <c>SalonContext.IsOwner</c> was <see langword="true"/>.</summary>
    OwnerContext,

    /// <summary>Real backend staff membership - the resolved <c>SalonContext.IsOwner</c> was <see langword="false"/>.</summary>
    StaffContext,

    /// <summary>
    /// Reserved for a signed-in account with real customer-facing activity
    /// (e.g. bookings, favorited/followed salons) but no salon ownership or
    /// staff membership. Not reachable by any code path in this phase -
    /// <c>GET /me/salon-access</c> returns identically empty
    /// <c>ownedSalons</c>/<c>memberships</c>/<c>specialistLinks</c> for such
    /// an account and for a genuinely brand-new user (confirmed live,
    /// repeatedly, across this project's sessions), so Desktop has no data
    /// today to distinguish the two - both resolve to
    /// <see cref="NoBusinessContext"/> instead. Making this reachable would
    /// require new customer-activity backend integration, out of this
    /// phase's scope (see the implementation report's own Known
    /// Limitations). Declared now, not fabricated later, so the eventual
    /// consumer (whoever adds that integration) extends this enum's
    /// meaning rather than inventing a new one - same "reserve the state
    /// rather than fabricate the distinction" precedent
    /// ROJAN_Phase2B_Context_State_Hardening_Plan_v1.md already set for
    /// its own reserved "AmbiguousContext".
    /// </summary>
    CustomerContext,

    /// <summary>
    /// No real backend context resolved (<c>GetCurrentContextAsync</c>
    /// returned <see langword="null"/>), and <see cref="IDemoModeProvider.IsEnabled"/>
    /// was <see langword="false"/>. The safe default for every account this
    /// phase cannot yet place more specifically - see <see cref="CustomerContext"/>.
    /// </summary>
    NoBusinessContext,

    /// <summary>
    /// The local seeded organization - reachable only when
    /// <see cref="IDemoModeProvider.IsEnabled"/> is explicitly
    /// <see langword="true"/>, checked before any real resolution is even
    /// attempted. Never a silent fallback - the exact P0 risk this phase
    /// exists to remove.
    /// </summary>
    DemoContext,
}
