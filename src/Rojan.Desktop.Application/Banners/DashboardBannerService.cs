using Rojan.Desktop.Domain.Banners;

namespace Rojan.Desktop.Application.Banners;

/// <summary>
/// Default <see cref="IDashboardBannerService"/>. Picks the first active Desktop banner by display
/// order - the same order the Admin Panel's reorder action sets - and loads its image. A missing
/// image is not a failure: the banner is still returned, without image bytes, so the Dashboard can
/// show its fallback surface instead of a broken image.
/// </summary>
public sealed class DashboardBannerService(IBannerRepository repository) : IDashboardBannerService
{
    public async Task<DashboardBannerDto?> GetDashboardBannerAsync(CancellationToken cancellationToken = default)
    {
        var banners = await repository.GetActiveDesktopBannersAsync(cancellationToken).ConfigureAwait(false);
        var banner = banners.OrderBy(candidate => candidate.DisplayOrder).FirstOrDefault();
        if (banner is null)
        {
            return null;
        }

        var imageBytes = string.IsNullOrWhiteSpace(banner.ImageUrl)
            ? null
            : await repository.GetImageAsync(banner.ImageUrl, cancellationToken).ConfigureAwait(false);

        return new DashboardBannerDto(banner.Id, banner.Title, banner.Subtitle, banner.Href, imageBytes);
    }
}
