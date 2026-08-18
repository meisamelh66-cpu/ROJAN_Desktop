using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Infrastructure.Salons;

/// <summary>
/// Owner App Booking Integration: the real, backend-connected
/// <see cref="ISalonContextService"/>. Calls
/// <c>GET /api/v1/users/me/salon-access</c> once and caches the result for
/// this instance's lifetime (registered as a DI singleton, same lifetime as
/// <c>BackendBookingRepository</c>) - which salon an owner manages does not
/// change mid-session, so there is no reason to re-fetch on every booking
/// call. <see cref="_resolveLock"/> guards the lazy first resolution
/// against two callers racing on startup (e.g. the Bookings page and a
/// future Customers/Calendar page both loading at once) - only one of them
/// actually calls the backend.
///
/// If the owner manages more than one salon, the first one the backend
/// returns is used. There is no salon-switcher UI in this phase - a known,
/// explicitly-flagged Phase 1 limitation (see
/// <c>ROJAN_Booking_Integration_Implementation_Report_v1.md</c>), not
/// something silently decided here.
///
/// Phase 1 Context Source Alignment: previously called
/// <c>GET /api/v1/salons/mine</c> (ownership only) and fell back to
/// <see cref="IAcceptedMembershipStore"/> for the staff case, since that
/// endpoint carried no membership data at all. <c>/me/salon-access</c>
/// carries the caller's owned salons *and* active staff memberships in one
/// response, so <see cref="ResolveAsync"/> now checks owned salons, then
/// the backend's own membership list, before falling back to
/// <see cref="IAcceptedMembershipStore"/> - which remains as the last
/// resort for a membership the backend response doesn't (yet) reflect.
/// Specialist links are present in the response but not resolved into a
/// <see cref="SalonContext"/> here - no existing code path resolved a
/// specialist before this change either, and adding one is a new
/// capability, not a source swap (deferred to a future phase - see
/// <c>ROJAN_Phase1_Context_Source_Alignment_Plan_v1.md</c>). Both
/// <see cref="GetSalonIdAsync"/> and <see cref="GetCurrentContextAsync"/>
/// share this one resolution method and its one cache, so they can never
/// disagree with each other or make separate backend calls.
/// </summary>
public sealed class BackendSalonContextService(IApiClient apiClient, IAcceptedMembershipStore acceptedMembershipStore) : ISalonContextService, IDisposable
{
    private const string SalonAccessPath = "/api/v1/users/me/salon-access";

    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private SalonContext? _resolvedContext;
    private bool _hasResolved;

    public async Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default)
    {
        var context = await ResolveAsync(cancellationToken).ConfigureAwait(false);
        return context?.SalonId;
    }

    public async Task<SalonContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default) =>
        await ResolveAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>Phase 1.2: resets the cache so the next resolution re-runs from the backend/local store - see the interface's own doc comment for why this exists.</summary>
    public void Invalidate()
    {
        _hasResolved = false;
        _resolvedContext = null;
    }

    private async Task<SalonContext?> ResolveAsync(CancellationToken cancellationToken)
    {
        if (_hasResolved)
        {
            return _resolvedContext;
        }

        await _resolveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_hasResolved)
            {
                return _resolvedContext;
            }

            var response = await apiClient.GetAsync<SalonAccessResponse>(SalonAccessPath, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess || response.Data is null)
            {
                throw new ApiException($"Failed to resolve the signed-in user's salon (status {response.StatusCode}): {response.ErrorMessage}");
            }

            if (response.Data.OwnedSalons.Count > 0)
            {
                var owned = response.Data.OwnedSalons[0];
                _resolvedContext = new SalonContext(owned.SalonId, owned.SalonName, IsOwner: true, MembershipRole: null);
            }
            else if (response.Data.Memberships.Count > 0)
            {
                var membership = response.Data.Memberships[0];
                _resolvedContext = new SalonContext(membership.SalonId, membership.SalonName, IsOwner: false, membership.Role);
            }
            else
            {
                var accepted = await acceptedMembershipStore.GetAsync(cancellationToken).ConfigureAwait(false);
                _resolvedContext = accepted is null
                    ? null
                    : new SalonContext(accepted.SalonId, accepted.SalonName, IsOwner: false, accepted.Role);
            }

            _hasResolved = true;
            return _resolvedContext;
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    public void Dispose() => _resolveLock.Dispose();
}
