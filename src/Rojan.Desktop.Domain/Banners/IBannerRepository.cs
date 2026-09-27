namespace Rojan.Desktop.Domain.Banners;

/// <summary>Read-only access to the banners the Admin Panel has published for the Desktop app.</summary>
public interface IBannerRepository
{
    /// <summary>Active Desktop banners, already filtered and ordered by the backend (display order ascending). Empty when none are published.</summary>
    public Task<IReadOnlyList<Banner>> GetActiveDesktopBannersAsync(CancellationToken cancellationToken = default);

    /// <summary>The banner image bytes, or <see langword="null"/> when the image cannot be fetched - callers fall back to an image-less banner surface.</summary>
    public Task<byte[]?> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default);
}
