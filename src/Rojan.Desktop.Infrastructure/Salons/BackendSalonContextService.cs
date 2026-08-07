using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
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
/// something silently decided here. If the owner manages zero salons,
/// <see cref="GetSalonIdAsync"/> returns <see langword="null"/> and
/// <c>BackendBookingRepository</c> surfaces a clear error rather than
/// guessing.
/// </summary>
public sealed class BackendSalonContextService(IApiClient apiClient) : ISalonContextService, IDisposable
{
    private const string MineSalonsPath = "/api/v1/salons/mine";

    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private string? _resolvedSalonId;
    private bool _hasResolved;

    public async Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default)
    {
        if (_hasResolved)
        {
            return _resolvedSalonId;
        }

        await _resolveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_hasResolved)
            {
                return _resolvedSalonId;
            }

            var response = await apiClient.GetAsync<List<SalonResponse>>(MineSalonsPath, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess || response.Data is null)
            {
                throw new ApiException($"Failed to resolve the signed-in owner's salon (status {response.StatusCode}): {response.ErrorMessage}");
            }

            _resolvedSalonId = response.Data.Count > 0 ? response.Data[0].Id : null;
            _hasResolved = true;
            return _resolvedSalonId;
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    /// <summary>Phase 1.2: resets the cache so the next <see cref="GetSalonIdAsync"/> call re-resolves from the backend - see the interface's own doc comment for why this exists.</summary>
    public void Invalidate()
    {
        _hasResolved = false;
        _resolvedSalonId = null;
    }

    public void Dispose() => _resolveLock.Dispose();
}
