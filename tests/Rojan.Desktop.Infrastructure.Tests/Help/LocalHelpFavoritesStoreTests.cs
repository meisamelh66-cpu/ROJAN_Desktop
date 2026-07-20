using Rojan.Desktop.Infrastructure.Help;

namespace Rojan.Desktop.Infrastructure.Tests.Help;

/// <summary>Exercises <see cref="LocalHelpFavoritesStore"/> against a temp file - toggle semantics and cross-instance persistence.</summary>
public sealed class LocalHelpFavoritesStoreTests : IDisposable
{
    private readonly string _filePath;

    public LocalHelpFavoritesStoreTests()
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
    public async Task GetFavoriteTopicIdsAsync_NoPersistedFile_ReturnsEmptySet()
    {
        var store = new LocalHelpFavoritesStore(_filePath);

        var favorites = await store.GetFavoriteTopicIdsAsync();

        Assert.Empty(favorites);
    }

    [Fact]
    public async Task ToggleFavoriteAsync_NotYetFavorite_AddsItAndReturnsTrue()
    {
        var store = new LocalHelpFavoritesStore(_filePath);

        var isNowFavorite = await store.ToggleFavoriteAsync("help-customers");

        Assert.True(isNowFavorite);
        Assert.Contains("help-customers", await store.GetFavoriteTopicIdsAsync());
    }

    [Fact]
    public async Task ToggleFavoriteAsync_AlreadyFavorite_RemovesItAndReturnsFalse()
    {
        var store = new LocalHelpFavoritesStore(_filePath);
        await store.ToggleFavoriteAsync("help-customers");

        var isNowFavorite = await store.ToggleFavoriteAsync("help-customers");

        Assert.False(isNowFavorite);
        Assert.DoesNotContain("help-customers", await store.GetFavoriteTopicIdsAsync());
    }

    [Fact]
    public async Task ToggleFavoriteAsync_PersistsAcrossInstances()
    {
        var first = new LocalHelpFavoritesStore(_filePath);
        await first.ToggleFavoriteAsync("help-dashboard");

        var second = new LocalHelpFavoritesStore(_filePath);
        var favorites = await second.GetFavoriteTopicIdsAsync();

        Assert.Contains("help-dashboard", favorites);
    }
}
