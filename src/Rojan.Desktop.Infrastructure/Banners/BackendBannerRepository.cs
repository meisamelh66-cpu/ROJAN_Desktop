using System.Net.Http;
using System.Net.Http.Headers;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Domain.Banners;

namespace Rojan.Desktop.Infrastructure.Banners;

/// <summary>
/// Reads the Admin-Panel-managed Desktop banners from ROJAN_Backend's existing public banner
/// endpoint - the same banner module the Admin Panel (<c>/rojan-admin/banners</c>) writes to, so a
/// banner change needs no Desktop update. The image is fetched with a plain, unauthenticated
/// request: banner image URLs are public media URLs that may live on a different host than the API
/// (CDN/object storage), and the session's bearer token must never be sent to such a host.
/// </summary>
public sealed class BackendBannerRepository : IBannerRepository
{
    private const string ActiveDesktopBannersPath = "/api/v1/public/banners?target=DESKTOP";

    private static readonly HttpClient ImageClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    private readonly IApiClient _apiClient;
    private readonly Func<Uri, CancellationToken, Task<byte[]>> _downloadImage;

    public BackendBannerRepository(IApiClient apiClient)
        : this(apiClient, DownloadImageAsync)
    {
    }

    internal BackendBannerRepository(IApiClient apiClient, Func<Uri, CancellationToken, Task<byte[]>> downloadImage)
    {
        _apiClient = apiClient;
        _downloadImage = downloadImage;
    }

    // "no-cache" makes any proxy/CDN on the way revalidate with the origin, so an image the admin
    // replaced is never served stale (HttpClient itself keeps no response cache).
    private static async Task<byte[]> DownloadImageAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        using var response = await ImageClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Banner>> GetActiveDesktopBannersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _apiClient
            .GetAsync<List<BannerResponse>>(ActiveDesktopBannersPath, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to load Desktop banners (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return (response.Data ?? [])
            .Where(banner => banner.IsActive)
            .OrderBy(banner => banner.DisplayOrder)
            .Select(banner => new Banner(banner.Id, banner.Title, banner.Subtitle, banner.Href, banner.ImageUrl, banner.DisplayOrder))
            .ToList();
    }

    public async Task<byte[]?> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

#pragma warning disable CA1031 // An unavailable banner image is a normal, non-fatal state - the Dashboard shows its fallback surface.
        try
        {
            var bytes = await _downloadImage(uri, cancellationToken).ConfigureAwait(false);
            return bytes.Length == 0 ? null : bytes;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
#pragma warning restore CA1031
    }
}
