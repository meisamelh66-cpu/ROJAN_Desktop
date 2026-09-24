using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Media;

namespace Rojan.Desktop.Infrastructure.Media;

/// <summary>
/// Phase F (Salon Media Integration): the real, backend-connected
/// <see cref="ISalonMediaRepository"/> - calls ROJAN_Backend's existing
/// <c>GET /api/v1/salons/{salonId}/media?mediaType=...</c> (no new/duplicate
/// media API). <see cref="ISalonContextService"/> resolves the salon
/// exactly as every other <c>Backend*Repository</c> does (see
/// <c>Services.BackendServiceRepository.ResolveSalonIdAsync</c>'s own
/// convention, mirrored here).
/// </summary>
public sealed class BackendSalonMediaRepository(
    IApiClient apiClient,
    ISalonContextService salonContextService) : ISalonMediaRepository
{
    public async Task<IReadOnlyList<SalonMediaAsset>> GetBySalonAsync(SalonMediaCategory category, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var mediaType = MapCategory(category);

        var response = await apiClient
            .GetAsync<List<MediaAssetResponse>>($"/api/v1/salons/{salonId}/media?mediaType={mediaType}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to load {mediaType} media (status {response.StatusCode}): {response.ErrorMessage}");
        }

        // A salon with no logo/cover/gallery uploaded yet is a normal, empty state - ROJAN_Backend
        // returns an empty list, never a failure, for zero matching assets (see this repository's
        // own interface doc comment).
        var assets = response.Data ?? [];

        return assets
            .OrderBy(asset => asset.DisplayOrder)
            .Select(asset => new SalonMediaAsset(asset.Id, asset.Url, asset.OriginalName, asset.DisplayOrder))
            .ToList();
    }

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to load media for.");
    }

    private static string MapCategory(SalonMediaCategory category) => category switch
    {
        SalonMediaCategory.Logo => "LOGO",
        SalonMediaCategory.Cover => "COVER",
        SalonMediaCategory.Gallery => "GALLERY",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unmapped SalonMediaCategory."),
    };
}
