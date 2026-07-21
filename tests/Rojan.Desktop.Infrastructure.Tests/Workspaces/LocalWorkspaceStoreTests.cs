using Rojan.Desktop.Domain.Workspaces;
using Rojan.Desktop.Infrastructure.Workspaces;

namespace Rojan.Desktop.Infrastructure.Tests.Workspaces;

/// <summary>Exercises <see cref="LocalWorkspaceStore"/> against temp files - persistence round-trip (including polymorphic <see cref="PaneNode"/> serialization), active-workspace tracking, and the Recent Workspaces eviction cap.</summary>
public sealed class LocalWorkspaceStoreTests : IDisposable
{
    private readonly string _workspacesFilePath;
    private readonly string _stateFilePath;

    public LocalWorkspaceStoreTests()
    {
        var directory = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        _workspacesFilePath = Path.Combine(directory, "workspaces.json");
        _stateFilePath = Path.Combine(directory, "state.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_workspacesFilePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private LocalWorkspaceStore CreateStore() => new(_workspacesFilePath, _stateFilePath);

    private static WorkspaceLayout SimpleWorkspace(string id, string name = "Default") =>
        new(id, name, "dashboard", null, [], [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, IsDefault: true);

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var store = CreateStore();

        var all = await store.GetAllAsync();

        Assert.Empty(all);
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_RoundTripsAFlatWorkspace()
    {
        var store = CreateStore();
        var workspace = SimpleWorkspace("w1");

        await store.SaveAsync(workspace);
        var reloaded = await store.GetByIdAsync("w1");

        Assert.NotNull(reloaded);
        Assert.Equal(workspace.Name, reloaded!.Name);
        Assert.Equal(workspace.PrimaryModuleId, reloaded.PrimaryModuleId);
        Assert.True(reloaded.IsDefault);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_UpdatesRatherThanDuplicates()
    {
        var store = CreateStore();
        await store.SaveAsync(SimpleWorkspace("w1", "Original"));

        await store.SaveAsync(SimpleWorkspace("w1", "Renamed"));

        var all = await store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Renamed", all[0].Name);
    }

    [Fact]
    public async Task SaveAsync_WithNestedSplitAndLeafPaneTree_RoundTripsThroughPolymorphicSerialization()
    {
        var store = CreateStore();
        var tree = new PaneSplit(
            "split-1",
            PaneOrientation.Vertical,
            0.6,
            new PaneLeaf("leaf-1", ["dashboard", "customers"], "customers"),
            new PaneSplit("split-2", PaneOrientation.Horizontal, 0.4, new PaneLeaf("leaf-2", ["bookings"], "bookings"), new PaneLeaf("leaf-3", ["services"], "services")));
        var workspace = SimpleWorkspace("w1") with { SecondaryRoot = tree };

        await store.SaveAsync(workspace);
        var reloaded = await store.GetByIdAsync("w1");

        var outerSplit = Assert.IsType<PaneSplit>(reloaded!.SecondaryRoot);
        Assert.Equal(PaneOrientation.Vertical, outerSplit.Orientation);
        Assert.Equal(0.6, outerSplit.Ratio);
        var firstLeaf = Assert.IsType<PaneLeaf>(outerSplit.First);
        Assert.Equal(["dashboard", "customers"], firstLeaf.ModuleIds);
        Assert.Equal("customers", firstLeaf.ActiveModuleId);
        var innerSplit = Assert.IsType<PaneSplit>(outerSplit.Second);
        Assert.Equal(PaneOrientation.Horizontal, innerSplit.Orientation);
        Assert.Equal(["services"], ((PaneLeaf)innerSplit.Second).ModuleIds);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFromWorkspacesAndFromRecent()
    {
        var store = CreateStore();
        await store.SaveAsync(SimpleWorkspace("w1"));
        await store.RecordRecentWorkspaceAsync("w1");

        await store.DeleteAsync("w1");

        Assert.Empty(await store.GetAllAsync());
        Assert.DoesNotContain("w1", await store.GetRecentWorkspaceIdsAsync());
    }

    [Fact]
    public async Task ActiveWorkspaceId_DefaultsToNullThenPersistsAcrossInstances()
    {
        var store = CreateStore();
        Assert.Null(await store.GetActiveWorkspaceIdAsync());

        await store.SetActiveWorkspaceIdAsync("w1");

        var second = CreateStore();
        Assert.Equal("w1", await second.GetActiveWorkspaceIdAsync());
    }

    [Fact]
    public async Task RecordRecentWorkspaceAsync_SameIdAgain_MovesToFrontWithoutDuplicating()
    {
        var store = CreateStore();
        await store.RecordRecentWorkspaceAsync("w1");
        await store.RecordRecentWorkspaceAsync("w2");

        await store.RecordRecentWorkspaceAsync("w1");

        var recent = await store.GetRecentWorkspaceIdsAsync();
        Assert.Equal(["w1", "w2"], recent);
    }

    [Fact]
    public async Task RecordRecentWorkspaceAsync_BeyondMaxRecentEntries_EvictsTheOldest()
    {
        var store = CreateStore();
        for (var i = 0; i < LocalWorkspaceStore.MaxRecentEntries + 2; i++)
        {
            await store.RecordRecentWorkspaceAsync($"w{i}");
        }

        var recent = await store.GetRecentWorkspaceIdsAsync();

        Assert.Equal(LocalWorkspaceStore.MaxRecentEntries, recent.Count);
        Assert.DoesNotContain("w0", recent);
    }
}
