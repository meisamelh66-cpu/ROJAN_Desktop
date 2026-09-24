using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Media;
using Rojan.Desktop.Infrastructure.Media;

namespace Rojan.Desktop.Infrastructure.Tests.Media;

/// <summary>
/// Exercises <see cref="BackendSalonMediaRepository"/> - the mediaType-filtered
/// GET, display-order sorting, the empty-vs-error distinction, and the
/// "no salon yet" guard. Only the HTTP transport (<see cref="IApiClient"/>)
/// is faked - same "exercise the real workflow" convention as
/// <c>Services.BackendServiceRepositoryTests</c>.
/// </summary>
public sealed class BackendSalonMediaRepositoryTests
{
    private const string SalonId = "salon-1";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task GetBySalonAsync_Logo_QueriesTheLogoMediaTypeAndMapsTheRealFields()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/media?mediaType=LOGO"] = new List<MediaAssetResponse>
        {
            new("media-1", SalonId, "LOGO", "logo.png", "image/png", 2048, "ACTIVE", "https://cdn.example/logo.png", Now, null, 0),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var assets = await repository.GetBySalonAsync(SalonMediaCategory.Logo);

        var asset = Assert.Single(assets);
        Assert.Equal("media-1", asset.Id);
        Assert.Equal("https://cdn.example/logo.png", asset.Url);
        Assert.Equal("logo.png", asset.OriginalName);
        Assert.Equal(0, asset.DisplayOrder);
    }

    [Theory]
    [InlineData(SalonMediaCategory.Cover, "COVER")]
    [InlineData(SalonMediaCategory.Gallery, "GALLERY")]
    public async Task GetBySalonAsync_OtherCategories_QueryTheMatchingMediaType(SalonMediaCategory category, string expectedMediaType)
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/media?mediaType={expectedMediaType}"] = new List<MediaAssetResponse>();

        var repository = CreateRepository(apiClient, SalonId);

        var assets = await repository.GetBySalonAsync(category);

        Assert.Empty(assets);
    }

    [Fact]
    public async Task GetBySalonAsync_GallerySortedByDisplayOrder_ReturnsInOrder_RegardlessOfWireOrder()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/media?mediaType=GALLERY"] = new List<MediaAssetResponse>
        {
            new("media-3", SalonId, "GALLERY", "third.jpg", "image/jpeg", 100, "ACTIVE", "https://cdn.example/3.jpg", Now, null, 2),
            new("media-1", SalonId, "GALLERY", "first.jpg", "image/jpeg", 100, "ACTIVE", "https://cdn.example/1.jpg", Now, null, 0),
            new("media-2", SalonId, "GALLERY", "second.jpg", "image/jpeg", 100, "ACTIVE", "https://cdn.example/2.jpg", Now, null, 1),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var assets = await repository.GetBySalonAsync(SalonMediaCategory.Gallery);

        Assert.Equal(["media-1", "media-2", "media-3"], assets.Select(a => a.Id));
    }

    [Fact]
    public async Task GetBySalonAsync_NoAssetsUploadedYet_ReturnsEmpty_NotAnError()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/media?mediaType=LOGO"] = new List<MediaAssetResponse>();

        var repository = CreateRepository(apiClient, SalonId);

        var assets = await repository.GetBySalonAsync(SalonMediaCategory.Logo);

        Assert.Empty(assets);
    }

    [Fact]
    public async Task GetBySalonAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetBySalonAsync(SalonMediaCategory.Logo));
    }

    [Fact]
    public async Task GetBySalonAsync_BackendFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/media?mediaType=LOGO"] = (500, "Server error");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetBySalonAsync(SalonMediaCategory.Logo));
    }

    private static BackendSalonMediaRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId));

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
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
            throw new NotSupportedException("BackendSalonMediaRepository never posts.");

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonMediaRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonMediaRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonMediaRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonMediaRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonMediaRepository never fetches raw bytes.");
    }
}
