using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Infrastructure.Banners;

namespace Rojan.Desktop.Infrastructure.Tests.Banners;

/// <summary>
/// Exercises <see cref="BackendBannerRepository"/> against the existing public banner endpoint the
/// Admin Panel's Desktop banners are served from. Only the HTTP transport (<see cref="IApiClient"/>)
/// and the unauthenticated image download are faked.
/// </summary>
public sealed class BackendBannerRepositoryTests
{
    private const string BannersPath = "/api/v1/public/banners?target=DESKTOP";

    private static BannerResponse Response(string id, int displayOrder, bool isActive = true, string imageUrl = "https://cdn.example/b.png") =>
        new(id, "DESKTOP", $"Title {id}", null, null, imageUrl, isActive, displayOrder, DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task GetActiveDesktopBannersAsync_ReturnsActiveBannersOnly_OrderedByDisplayOrder()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[BannersPath] = new List<BannerResponse> { Response("c", 3), Response("off", 0, isActive: false), Response("a", 1) };
        var repository = new BackendBannerRepository(apiClient, (_, _) => Task.FromResult(Array.Empty<byte>()));

        var banners = await repository.GetActiveDesktopBannersAsync();

        Assert.Equal(["a", "c"], banners.Select(banner => banner.Id));
        Assert.Equal("Title a", banners[0].Title);
    }

    [Fact]
    public async Task GetActiveDesktopBannersAsync_RequestFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[BannersPath] = (503, "unavailable");
        var repository = new BackendBannerRepository(apiClient, (_, _) => Task.FromResult(Array.Empty<byte>()));

        await Assert.ThrowsAsync<ApiException>(() => repository.GetActiveDesktopBannersAsync());
    }

    [Fact]
    public async Task GetImageAsync_DownloadsTheAbsoluteUrl()
    {
        Uri? requested = null;
        var repository = new BackendBannerRepository(new StubApiClient(), (uri, _) =>
        {
            requested = uri;
            return Task.FromResult(new byte[] { 1, 2 });
        });

        var bytes = await repository.GetImageAsync("https://cdn.example/banner.png");

        Assert.Equal(new byte[] { 1, 2 }, bytes);
        Assert.Equal(new Uri("https://cdn.example/banner.png"), requested);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/relative/banner.png")]
    [InlineData("file:///C:/banner.png")]
    public async Task GetImageAsync_NotAnAbsoluteHttpUrl_ReturnsNullWithoutDownloading(string imageUrl)
    {
        var downloaded = false;
        var repository = new BackendBannerRepository(new StubApiClient(), (_, _) =>
        {
            downloaded = true;
            return Task.FromResult(new byte[] { 1 });
        });

        Assert.Null(await repository.GetImageAsync(imageUrl));
        Assert.False(downloaded);
    }

    [Fact]
    public async Task GetImageAsync_DownloadFails_ReturnsNull()
    {
        var repository = new BackendBannerRepository(new StubApiClient(), (_, _) => Task.FromException<byte[]>(new HttpRequestException("404")));

        Assert.Null(await repository.GetImageAsync("https://cdn.example/banner.png"));
    }

    [Fact]
    public async Task GetImageAsync_EmptyBody_ReturnsNull()
    {
        var repository = new BackendBannerRepository(new StubApiClient(), (_, _) => Task.FromResult(Array.Empty<byte>()));

        Assert.Null(await repository.GetImageAsync("https://cdn.example/banner.png"));
    }

    [Fact]
    public async Task GetImageAsync_CallerCancels_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var repository = new BackendBannerRepository(new StubApiClient(), (_, token) => Task.FromCanceled<byte[]>(token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetImageAsync("https://cdn.example/banner.png", cts.Token));
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            if (GetFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            if (GetResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 200));
            }

            throw new InvalidOperationException($"Unexpected GET '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBannerRepository never posts.");

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBannerRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBannerRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBannerRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBannerRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBannerRepository fetches images without the API client.");
    }
}
