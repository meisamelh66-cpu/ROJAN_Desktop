using Rojan.Desktop.Infrastructure.Search;

namespace Rojan.Desktop.Infrastructure.Tests.Search;

/// <summary>Exercises <see cref="LocalSearchFavoritesStore"/> against a temp file - toggle semantics and cross-instance persistence.</summary>
public sealed class LocalSearchFavoritesStoreTests : IDisposable
{
    private readonly string _filePath;

    public LocalSearchFavoritesStoreTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "favorites.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task GetFavoriteIdsAsync_NoPersistedFile_ReturnsEmptySet()
    {
        var store = new LocalSearchFavoritesStore(_filePath);

        var favorites = await store.GetFavoriteIdsAsync();

        Assert.Empty(favorites);
    }

    [Fact]
    public async Task ToggleFavoriteAsync_NotYetFavorite_AddsItAndReturnsTrue()
    {
        var store = new LocalSearchFavoritesStore(_filePath);

        var isNowFavorite = await store.ToggleFavoriteAsync("customer:c1");

        Assert.True(isNowFavorite);
        Assert.Contains("customer:c1", await store.GetFavoriteIdsAsync());
    }

    [Fact]
    public async Task ToggleFavoriteAsync_AlreadyFavorite_RemovesItAndReturnsFalse()
    {
        var store = new LocalSearchFavoritesStore(_filePath);
        await store.ToggleFavoriteAsync("customer:c1");

        var isNowFavorite = await store.ToggleFavoriteAsync("customer:c1");

        Assert.False(isNowFavorite);
        Assert.DoesNotContain("customer:c1", await store.GetFavoriteIdsAsync());
    }

    [Fact]
    public async Task ToggleFavoriteAsync_PersistsAcrossInstances()
    {
        var first = new LocalSearchFavoritesStore(_filePath);
        await first.ToggleFavoriteAsync("page:bookings");

        var second = new LocalSearchFavoritesStore(_filePath);
        var favorites = await second.GetFavoriteIdsAsync();

        Assert.Contains("page:bookings", favorites);
    }
}
