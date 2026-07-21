using Rojan.Desktop.Infrastructure.Search;

namespace Rojan.Desktop.Infrastructure.Tests.Search;

/// <summary>Exercises <see cref="LocalSearchHistoryStore"/> against a temp file - persistence round-trip, case-insensitive dedup-and-move-to-front, and the <see cref="LocalSearchHistoryStore.MaxEntries"/> eviction cap.</summary>
public sealed class LocalSearchHistoryStoreTests : IDisposable
{
    private readonly string _filePath;

    public LocalSearchHistoryStoreTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "history.json");
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
    public async Task GetRecentSearchesAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var store = new LocalSearchHistoryStore(_filePath);

        var recent = await store.GetRecentSearchesAsync();

        Assert.Empty(recent);
    }

    [Fact]
    public async Task RecordSearchAsync_InsertsAtTheFront()
    {
        var store = new LocalSearchHistoryStore(_filePath);

        await store.RecordSearchAsync("sarah");
        await store.RecordSearchAsync("bookings");

        var recent = await store.GetRecentSearchesAsync();
        Assert.Equal(["bookings", "sarah"], recent);
    }

    [Fact]
    public async Task RecordSearchAsync_SameQueryDifferentCase_DedupesAndMovesToFront()
    {
        var store = new LocalSearchHistoryStore(_filePath);
        await store.RecordSearchAsync("Sarah");
        await store.RecordSearchAsync("bookings");

        await store.RecordSearchAsync("SARAH");

        var recent = await store.GetRecentSearchesAsync();
        Assert.Equal(["SARAH", "bookings"], recent);
    }

    [Fact]
    public async Task RecordSearchAsync_BeyondMaxEntries_EvictsTheOldest()
    {
        var store = new LocalSearchHistoryStore(_filePath);
        for (var i = 0; i < LocalSearchHistoryStore.MaxEntries + 2; i++)
        {
            await store.RecordSearchAsync($"query{i}");
        }

        var recent = await store.GetRecentSearchesAsync();

        Assert.Equal(LocalSearchHistoryStore.MaxEntries, recent.Count);
        Assert.DoesNotContain("query0", recent);
        Assert.Contains($"query{LocalSearchHistoryStore.MaxEntries + 1}", recent);
    }

    [Fact]
    public async Task ClearAsync_RemovesEveryEntry()
    {
        var store = new LocalSearchHistoryStore(_filePath);
        await store.RecordSearchAsync("sarah");
        await store.RecordSearchAsync("bookings");

        await store.ClearAsync();

        Assert.Empty(await store.GetRecentSearchesAsync());
    }

    [Fact]
    public async Task RecordSearchAsync_PersistsAcrossInstances()
    {
        var first = new LocalSearchHistoryStore(_filePath);
        await first.RecordSearchAsync("sarah");

        var second = new LocalSearchHistoryStore(_filePath);
        var recent = await second.GetRecentSearchesAsync();

        Assert.Single(recent);
        Assert.Equal("sarah", recent[0]);
    }
}
