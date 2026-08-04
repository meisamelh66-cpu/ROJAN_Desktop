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
}
