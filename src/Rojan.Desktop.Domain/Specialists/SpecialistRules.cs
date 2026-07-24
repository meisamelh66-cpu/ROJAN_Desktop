namespace Rojan.Desktop.Domain.Specialists;

/// <summary>
/// Validation rules for the specialist employment lifecycle - the same
/// deliberate deviation from this codebase's usual "Domain is just data +
/// repository contract" minimalism that <c>Bookings.BookingRules</c>/
/// <c>Customers.CustomerRules</c>/<c>Services.ServiceRules</c> already
/// established. Unlike <c>Services.ServiceRules</c> (added ahead of any
/// Application-layer consumer), <c>Application.Specialists.SpecialistCommandService.UpdateSpecialistAsync</c>
/// already accepts a caller-supplied status on every update with zero
/// validation today - the exact gap <c>Customers.CustomerRules</c> closed
/// for <c>CustomerCommandService.UpdateCustomerAsync</c> in Sprint 4
/// Commit 1, now closed here the same way.
///
/// Unlike <see cref="Bookings.BookingRules"/>, no status here is fully
/// terminal - same reasoning as <see cref="Customers.CustomerRules"/>/
/// <see cref="Services.ServiceRules"/>: an employment relationship is
/// ongoing, not a one-shot event, so an <see cref="SpecialistStatus.Inactive"/>
/// (archived/former) specialist can always be reactivated
/// (<see cref="SpecialistStatus.Inactive"/> -&gt; <see cref="SpecialistStatus.Active"/>)
/// rather than being permanently locked out.
/// </summary>
public static class SpecialistRules
{
    private static readonly Dictionary<SpecialistStatus, SpecialistStatus[]> ValidTransitions = new()
    {
        [SpecialistStatus.Active] = [SpecialistStatus.OnLeave, SpecialistStatus.Inactive],
        [SpecialistStatus.OnLeave] = [SpecialistStatus.Active, SpecialistStatus.Inactive],
        [SpecialistStatus.Inactive] = [SpecialistStatus.Active],
    };

    /// <summary>Whether moving a specialist directly from <paramref name="from"/> to <paramref name="to"/> is legal. Callers must only invoke this for an actual status change (<paramref name="from"/> != <paramref name="to"/>) - editing a specialist's other fields (name/title/email/phone/bio) without changing status is not a transition at all and must never be rejected here.</summary>
    public static bool IsValidTransition(SpecialistStatus from, SpecialistStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>
    /// Whether a specialist currently in <paramref name="from"/> can be
    /// activated (moved to <see cref="SpecialistStatus.Active"/>) - both
    /// non-Active statuses (OnLeave, Inactive) allow it per
    /// <see cref="IsValidTransition"/>; an already-Active specialist is
    /// explicitly excluded, since "activating" someone already active is
    /// not a real activation and callers should not present it as one
    /// (e.g. an enabled "Activate" button/command).
    /// </summary>
    public static bool CanActivate(SpecialistStatus from) =>
        from != SpecialistStatus.Active && IsValidTransition(from, SpecialistStatus.Active);

    /// <summary>
    /// Whether a specialist currently in <paramref name="from"/> can be
    /// deactivated - a temporary pause (moved to
    /// <see cref="SpecialistStatus.OnLeave"/>), distinct from
    /// <see cref="CanArchive"/>'s more final "no longer employed" move.
    /// Only a currently-Active specialist can be "deactivated" at all; an
    /// OnLeave specialist is already paused and an Inactive one is already
    /// archived, so this correctly returns false for both.
    /// </summary>
    public static bool CanDeactivate(SpecialistStatus from) =>
        from == SpecialistStatus.Active;

    /// <summary>
    /// Whether a specialist currently in <paramref name="from"/> can be
    /// archived (moved to <see cref="SpecialistStatus.Inactive"/>) - both
    /// Active and OnLeave allow it per <see cref="IsValidTransition"/>; an
    /// already-Inactive specialist cannot be archived again.
    /// </summary>
    public static bool CanArchive(SpecialistStatus from) =>
        from != SpecialistStatus.Inactive && IsValidTransition(from, SpecialistStatus.Inactive);

    /// <summary>Whether <paramref name="status"/> represents an archived (no longer employed) specialist.</summary>
    public static bool IsArchived(SpecialistStatus status) =>
        status == SpecialistStatus.Inactive;
}
