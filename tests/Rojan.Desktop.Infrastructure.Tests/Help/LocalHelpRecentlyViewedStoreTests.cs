using Rojan.Desktop.Infrastructure.Help;

namespace Rojan.Desktop.Infrastructure.Tests.Help;

/// <summary>Exercises <see cref="LocalHelpRecentlyViewedStore"/> against a temp file - most-recent-first ordering, re-viewing moves an entry to the front, and the <see cref="LocalHelpRecentlyViewedStore.MaxEntries"/> eviction cap.</summary>
public sealed class LocalHelpRecentlyViewedStoreTests : IDisposable
{
    private readonly string _filePath;

    public LocalHelpRecentlyViewedStoreTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "recent.json");
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
    public async Task GetRecentTopicIdsAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var store = new LocalHelpRecentlyViewedStore(_filePath);

        var recent = await store.GetRecentTopicIdsAsync();

        Assert.Empty(recent);
    }

    [Fact]
    public async Task RecordViewedAsync_AddsToTheFront()
    {
        var store = new LocalHelpRecentlyViewedStore(_filePath);

        await store.RecordViewedAsync("help-customers");
        await store.RecordViewedAsync("help-dashboard");

        Assert.Equal(["help-dashboard", "help-customers"], await store.GetRecentTopicIdsAsync());
    }

    [Fact]
    public async Task RecordViewedAsync_AlreadyPresent_MovesItToTheFrontInsteadOfDuplicating()
    {
        var store = new LocalHelpRecentlyViewedStore(_filePath);
        await store.RecordViewedAsync("help-customers");
        await store.RecordViewedAsync("help-dashboard");

        await store.RecordViewedAsync("help-customers");

        Assert.Equal(["help-customers", "help-dashboard"], await store.GetRecentTopicIdsAsync());
    }

    [Fact]
    public async Task RecordViewedAsync_BeyondMaxEntries_EvictsTheOldest()
    {
        var store = new LocalHelpRecentlyViewedStore(_filePath);
        for (var i = 0; i < LocalHelpRecentlyViewedStore.MaxEntries + 2; i++)
        {
            await store.RecordViewedAsync($"topic-{i}");
        }

        var recent = await store.GetRecentTopicIdsAsync();

        Assert.Equal(LocalHelpRecentlyViewedStore.MaxEntries, recent.Count);
        Assert.DoesNotContain("topic-0", recent);
        Assert.DoesNotContain("topic-1", recent);
        Assert.Contains($"topic-{LocalHelpRecentlyViewedStore.MaxEntries + 1}", recent);
    }

    [Fact]
    public async Task RecordViewedAsync_PersistsAcrossInstances()
    {
        var first = new LocalHelpRecentlyViewedStore(_filePath);
        await first.RecordViewedAsync("help-dashboard");

        var second = new LocalHelpRecentlyViewedStore(_filePath);
        var recent = await second.GetRecentTopicIdsAsync();

        Assert.Contains("help-dashboard", recent);
    }
}
