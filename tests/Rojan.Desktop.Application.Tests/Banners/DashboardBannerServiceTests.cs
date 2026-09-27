using Rojan.Desktop.Application.Banners;
using Rojan.Desktop.Domain.Banners;

namespace Rojan.Desktop.Application.Tests.Banners;

public sealed class DashboardBannerServiceTests
{
    [Fact]
    public async Task GetDashboardBannerAsync_PicksTheLowestDisplayOrder_AndLoadsItsImage()
    {
        var repository = new StubBannerRepository(
        [
            new Banner("second", "Second", null, null, "https://cdn.example/second.png", 2),
            new Banner("first", "First", "Sub", "https://rojan.example", "https://cdn.example/first.png", 1),
        ]);
        repository.Images["https://cdn.example/first.png"] = [9, 9];

        var banner = await new DashboardBannerService(repository).GetDashboardBannerAsync();

        Assert.NotNull(banner);
        Assert.Equal("first", banner.Id);
        Assert.Equal("First", banner.Title);
        Assert.Equal("Sub", banner.Subtitle);
        Assert.Equal("https://rojan.example", banner.Href);
        Assert.Equal(new byte[] { 9, 9 }, banner.ImageBytes);
        Assert.Equal(["https://cdn.example/first.png"], repository.RequestedImages);
    }

    [Fact]
    public async Task GetDashboardBannerAsync_NoActiveBanner_ReturnsNull()
    {
        var banner = await new DashboardBannerService(new StubBannerRepository([])).GetDashboardBannerAsync();

        Assert.Null(banner);
    }

    [Fact]
    public async Task GetDashboardBannerAsync_ImageUnavailable_StillReturnsTheBannerWithoutImage()
    {
        var repository = new StubBannerRepository([new Banner("b", "Title", null, null, "https://cdn.example/missing.png", 0)]);

        var banner = await new DashboardBannerService(repository).GetDashboardBannerAsync();

        Assert.NotNull(banner);
        Assert.Null(banner.ImageBytes);
    }

    [Fact]
    public async Task GetDashboardBannerAsync_BlankImageUrl_DoesNotRequestAnImage()
    {
        var repository = new StubBannerRepository([new Banner("b", "Title", null, null, " ", 0)]);

        var banner = await new DashboardBannerService(repository).GetDashboardBannerAsync();

        Assert.NotNull(banner);
        Assert.Empty(repository.RequestedImages);
    }

    [Fact]
    public async Task GetDashboardBannerAsync_ListFails_PropagatesSoTheCallerCanHideTheBanner()
    {
        var repository = new StubBannerRepository([]) { ListFailure = new InvalidOperationException("down") };

        await Assert.ThrowsAsync<InvalidOperationException>(() => new DashboardBannerService(repository).GetDashboardBannerAsync());
    }

    private sealed class StubBannerRepository(IReadOnlyList<Banner> banners) : IBannerRepository
    {
        public Dictionary<string, byte[]> Images { get; } = [];

        public List<string> RequestedImages { get; } = [];

        public Exception? ListFailure { get; init; }

        public Task<IReadOnlyList<Banner>> GetActiveDesktopBannersAsync(CancellationToken cancellationToken = default) =>
            ListFailure is null ? Task.FromResult(banners) : Task.FromException<IReadOnlyList<Banner>>(ListFailure);

        public Task<byte[]?> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            RequestedImages.Add(imageUrl);
            return Task.FromResult(Images.TryGetValue(imageUrl, out var bytes) ? bytes : null);
        }
    }
}
