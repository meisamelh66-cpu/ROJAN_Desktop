namespace Rojan.Desktop.Application.Salons;

/// <summary>
/// Owner App Booking Integration: resolves which ROJAN_Backend salon the
/// currently signed-in owner manages - <c>BackendBookingRepository</c>
/// needs this for every salon-scoped call (<c>GET /salons/{salonId}/bookings</c>,
/// <c>POST /bookings</c>'s implied <c>salonId</c>, etc.), and nothing in
/// the Owner App resolved this before now: <c>IEnterpriseContext</c>'s
/// <c>CurrentOrganizationId</c>/<c>CurrentBranchId</c> come entirely from
/// local fake data, not the backend. Deliberately a small, narrowly-scoped
/// new port rather than extending <c>IEnterpriseContext</c> itself - this
/// is Booking-integration-specific plumbing, not a redesign of the
/// existing Organization/Branch model.
/// </summary>
public interface ISalonContextService
{
    /// <summary>
    /// The signed-in owner's salon id, or <see langword="null"/> if they
    /// own no salon yet. If the owner manages more than one salon, the
    /// first one returned by the backend is used - there is no salon
    /// switcher UI yet (a known Phase 1 limitation, not silently ignored -
    /// see the concrete implementation's own doc comment).
    /// </summary>
    public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 1.2 Owner App Create Salon Flow: forces the next
    /// <see cref="GetSalonIdAsync"/> call to re-resolve from the backend
    /// rather than returning a cached result - <c>Salons.SalonCommandService</c>
    /// calls this immediately after a successful create, so every other
    /// already-loaded module picks up the new salon on its next read
    /// without requiring an app restart (the concrete implementation
    /// otherwise caches "no salon" for its entire singleton lifetime, which
    /// would silently strand a brand-new owner even after they finish
    /// onboarding). Defaults to a no-op so every existing
    /// <see cref="ISalonContextService"/> test double keeps compiling
    /// unchanged - only the real, caching implementation needs to override
    /// it. Reception Production Integration: the Accept Invite flow
    /// (<c>Presentation.ViewModels.Membership.AcceptInviteViewModel</c>)
    /// calls this too, for the identical reason - a brand-new Reception
    /// member's salon must be picked up without a restart, same as a
    /// brand-new owner's.
    /// </summary>
    public void Invalidate()
    {
    }

    /// <summary>
    /// Reception Production Integration: the fuller picture
    /// <see cref="GetSalonIdAsync"/>'s bare id doesn't carry - which salon
    /// (name, not just id) and whether the caller reached it through
    /// ownership or an accepted <c>SalonInvite</c> membership (and which
    /// role, if the latter). <c>Shell.Organizations.CurrentSessionService</c>
    /// is the only caller - every other module keeps depending on
    /// <see cref="GetSalonIdAsync"/> alone, unaffected by this addition.
    /// Resolves from the exact same cached lookup <see cref="GetSalonIdAsync"/>
    /// uses - never a second, independent backend call. Defaults to
    /// <see langword="null"/> for the same "existing test doubles keep
    /// compiling unchanged" reason <see cref="Invalidate"/> already
    /// documents - only the real implementation needs to override it.
    /// </summary>
    public Task<SalonContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<SalonContext?>(null);
}

/// <summary>See <see cref="ISalonContextService.GetCurrentContextAsync"/>. <see cref="IsOwner"/> true means <see cref="MembershipRole"/> is always null (an owner's role is never a <c>SalonRole</c> membership - see ROJAN_Backend's own <c>SalonRole</c> doc comment); false means <see cref="MembershipRole"/> carries the raw backend role string (<c>"MANAGER"</c>/<c>"RECEPTIONIST"</c>).</summary>
public sealed record SalonContext(string SalonId, string SalonName, bool IsOwner, string? MembershipRole);
