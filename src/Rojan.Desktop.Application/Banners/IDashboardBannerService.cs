namespace Rojan.Desktop.Application.Banners;

/// <summary>Resolves the Dashboard's hero banner from the Admin-Panel-managed Desktop banners.</summary>
public interface IDashboardBannerService
{
    /// <summary>The first active Desktop banner by display order (with its image when available), or <see langword="null"/> when none is published.</summary>
    public Task<DashboardBannerDto?> GetDashboardBannerAsync(CancellationToken cancellationToken = default);
}
