using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Infrastructure.Salons;

/// <summary>
/// Owner App Booking Integration: the real, backend-connected
/// <see cref="ISalonContextService"/>. Calls <c>GET /api/v1/salons/mine</c>
/// once and caches the result for this instance's lifetime (registered as
/// a DI singleton, same lifetime as <c>BackendBookingRepository</c>) -
/// which salon an owner manages does not change mid-session, so there is
/// no reason to re-fetch on every booking call. <see cref="_resolveLock"/>
/// guards the lazy first resolution against two callers racing on startup
/// (e.g. the Bookings page and a future Customers/Calendar page both
/// loading at once) - only one of them actually calls the backend.
///
/// If the owner manages more than one salon, the first one the backend
/// returns is used. There is no salon-switcher UI in this phase - a known,
/// explicitly-flagged Phase 1 limitation (see
/// <c>ROJAN_Booking_Integration_Implementation_Report_v1.md</c>), not
/// something silently decided here.
///
/// Reception Production Integration: if the caller owns no salon,
/// <see cref="ResolveAsync"/> now falls back to
/// <see cref="IAcceptedMembershipStore"/> before giving up - a Reception (or
/// Manager) member has no <c>GET /salons/mine</c> result at all (they don't
/// own anything), only a locally-persisted accepted invite. Both paths
/// share this one resolution method and its one cache, so
/// <see cref="GetSalonIdAsync"/> and <see cref="GetCurrentContextAsync"/>
/// can never disagree with each other or make separate backend calls.
/// </summary>
public sealed class BackendSalonContextService(IApiClient apiClient, IAcceptedMembershipStore acceptedMembershipStore) : ISalonContextService, IDisposable
{
    private const string MineSalonsPath = "/api/v1/salons/mine";

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

            var response = await apiClient.GetAsync<List<SalonResponse>>(MineSalonsPath, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess || response.Data is null)
            {
                throw new ApiException($"Failed to resolve the signed-in user's salon (status {response.StatusCode}): {response.ErrorMessage}");
            }

            if (response.Data.Count > 0)
            {
                var salon = response.Data[0];
                _resolvedContext = new SalonContext(salon.Id, salon.Name, IsOwner: true, MembershipRole: null);
            }
            else
            {
                var membership = await acceptedMembershipStore.GetAsync(cancellationToken).ConfigureAwait(false);
                _resolvedContext = membership is null
                    ? null
                    : new SalonContext(membership.SalonId, membership.SalonName, IsOwner: false, membership.Role);
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
