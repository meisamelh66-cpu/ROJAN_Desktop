namespace Rojan.Desktop.Domain.Media;

/// <summary>
/// Phase F (Salon Media Integration): read-only access to a salon's
/// logo/cover/gallery images, as ROJAN_Backend's <c>MediaController</c>
/// (<c>GET /api/v1/salons/{salonId}/media?mediaType=...</c>) already
/// exposes - no duplicate media API introduced here, this is purely a
/// Desktop-side read of the existing endpoint. Domain defines the
/// contract; Infrastructure provides the concrete implementation (<c>Infrastructure.Media.BackendSalonMediaRepository</c>),
/// same "no <c>salonId</c> parameter here - the implementation resolves it
/// internally via <c>Application.Salons.ISalonContextService</c>" shape as
/// <c>Services.IServiceRepository</c>.
///
/// Deliberately read-only: ROJAN_Backend's <c>MediaController</c> also
/// supports upload/delete/reorder, but nothing in this app has a media
/// upload UI yet - adding write methods here with no caller would be
/// speculative surface, not a real integration. A future upload feature
/// extends this interface then, not before.
/// </summary>
public interface ISalonMediaRepository
{
    /// <summary>
    /// Every active media asset of <paramref name="category"/> the current
    /// salon has, pre-sorted by display order - empty (never an error) if
    /// none have been uploaded yet, since that is ROJAN_Backend's own
    /// normal response for a salon with no matching assets, not a failure
    /// state.
    /// </summary>
    public Task<IReadOnlyList<SalonMediaAsset>> GetBySalonAsync(SalonMediaCategory category, CancellationToken cancellationToken = default);
}
