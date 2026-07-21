using Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Domain.Tests.Workspaces;

/// <summary>Exercises <see cref="WorkspaceRules"/>'s pure tree-manipulation logic - split/open-tab/close-tab/resize correctness, ratio/size clamping, and name normalization.</summary>
public sealed class WorkspaceRulesTests
{
    private static int _idCounter;

    private static string NewId() => $"id-{Interlocked.Increment(ref _idCounter)}";

    [Theory]
    [InlineData(0.5, 0.5)]
    [InlineData(0.05, WorkspaceRules.MinRatio)]
    [InlineData(0.99, WorkspaceRules.MaxRatio)]
    public void ClampRatio_OutOfRange_ClampsToMinOrMax(double input, double expected) =>
        Assert.Equal(expected, WorkspaceRules.ClampRatio(input));

    [Theory]
    [InlineData(300, 300)]
    [InlineData(10, WorkspaceRules.MinDockSize)]
    [InlineData(9999, WorkspaceRules.MaxDockSize)]
    public void ClampDockSize_OutOfRange_ClampsToMinOrMax(double input, double expected) =>
        Assert.Equal(expected, WorkspaceRules.ClampDockSize(input));

    [Theory]
    [InlineData(null, "Fallback")]
    [InlineData("", "Fallback")]
    [InlineData("   ", "Fallback")]
    [InlineData("  My Workspace  ", "My Workspace")]
    public void NormalizeName_BlankOrWhitespace_FallsBackOtherwiseTrims(string? name, string expected) =>
        Assert.Equal(expected, WorkspaceRules.NormalizeName(name, "Fallback"));

    [Fact]
    public void CreateDefault_ProducesEmptySingleModuleWorkspace()
    {
        var now = DateTimeOffset.UtcNow;

        var workspace = WorkspaceRules.CreateDefault("w1", "Default", "dashboard", now, isDefault: true);

        Assert.Equal("w1", workspace.Id);
        Assert.Equal("dashboard", workspace.PrimaryModuleId);
        Assert.Null(workspace.SecondaryRoot);
        Assert.Empty(workspace.DockedPanels);
        Assert.Empty(workspace.FloatingWindows);
        Assert.True(workspace.IsDefault);
    }

    [Fact]
    public void Split_FromNullRoot_CapturesPrimaryModuleAsFirstChild()
    {
        var result = WorkspaceRules.Split(null, targetLeafId: null, primaryModuleId: "dashboard", newModuleId: "customers", PaneOrientation.Horizontal, NewId);

        var split = Assert.IsType<PaneSplit>(result);
        var first = Assert.IsType<PaneLeaf>(split.First);
        var second = Assert.IsType<PaneLeaf>(split.Second);
        Assert.Equal(["dashboard"], first.ModuleIds);
        Assert.Equal(["customers"], second.ModuleIds);
        Assert.Equal(WorkspaceRules.DefaultRatio, split.Ratio);
    }

    [Fact]
    public void Split_TargetingExistingLeaf_NestsUnderThatLeaf()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);
        var firstLeafId = ((PaneLeaf)((PaneSplit)root).First).Id;

        var result = WorkspaceRules.Split(root, firstLeafId, "dashboard", "bookings", PaneOrientation.Vertical, NewId);

        var outer = Assert.IsType<PaneSplit>(result);
        var nested = Assert.IsType<PaneSplit>(outer.First);
        Assert.Equal(PaneOrientation.Vertical, nested.Orientation);
        Assert.Equal(["dashboard"], ((PaneLeaf)nested.First).ModuleIds);
        Assert.Equal(["bookings"], ((PaneLeaf)nested.Second).ModuleIds);
    }

    [Fact]
    public void Split_TargetLeafIdNotFound_FallsBackToFirstLeaf()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);

        var result = WorkspaceRules.Split(root, "not-a-real-leaf-id", "dashboard", "bookings", PaneOrientation.Horizontal, NewId);

        var outer = Assert.IsType<PaneSplit>(result);
        var nested = Assert.IsType<PaneSplit>(outer.First);
        Assert.Equal(["dashboard"], ((PaneLeaf)nested.First).ModuleIds);
    }

    [Fact]
    public void OpenTab_NewModule_AddsAndActivatesIt()
    {
        var leaf = new PaneLeaf("leaf-1", ["dashboard"], "dashboard");

        var result = (PaneLeaf)WorkspaceRules.OpenTab(leaf, "leaf-1", "customers");

        Assert.Equal(["dashboard", "customers"], result.ModuleIds);
        Assert.Equal("customers", result.ActiveModuleId);
    }

    [Fact]
    public void OpenTab_AlreadyOpenModule_ActivatesWithoutDuplicating()
    {
        var leaf = new PaneLeaf("leaf-1", ["dashboard", "customers"], "dashboard");

        var result = (PaneLeaf)WorkspaceRules.OpenTab(leaf, "leaf-1", "customers");

        Assert.Equal(["dashboard", "customers"], result.ModuleIds);
        Assert.Equal("customers", result.ActiveModuleId);
    }

    [Fact]
    public void SetActiveTab_ModuleNotOpenInLeaf_LeavesUnchanged()
    {
        var leaf = new PaneLeaf("leaf-1", ["dashboard"], "dashboard");

        var result = (PaneLeaf)WorkspaceRules.SetActiveTab(leaf, "leaf-1", "customers");

        Assert.Equal("dashboard", result.ActiveModuleId);
    }

    [Fact]
    public void CloseTab_ActiveTabClosed_ActivatesFirstRemaining()
    {
        var leaf = new PaneLeaf("leaf-1", ["dashboard", "customers", "bookings"], "customers");

        var result = (PaneLeaf)WorkspaceRules.CloseTab(leaf, "leaf-1", "customers")!;

        Assert.Equal(["dashboard", "bookings"], result.ModuleIds);
        Assert.Equal("dashboard", result.ActiveModuleId);
    }

    [Fact]
    public void CloseTab_InactiveTabClosed_ActiveModuleUnchanged()
    {
        var leaf = new PaneLeaf("leaf-1", ["dashboard", "customers"], "dashboard");

        var result = (PaneLeaf)WorkspaceRules.CloseTab(leaf, "leaf-1", "customers")!;

        Assert.Equal("dashboard", result.ActiveModuleId);
    }

    [Fact]
    public void CloseTab_LastTabInOnlyLeaf_CollapsesToNull()
    {
        var leaf = new PaneLeaf("leaf-1", ["dashboard"], "dashboard");

        var result = WorkspaceRules.CloseTab(leaf, "leaf-1", "dashboard");

        Assert.Null(result);
    }

    [Fact]
    public void CloseTab_LastTabInOneSideOfASplit_CollapsesToTheSibling()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);
        var secondLeafId = ((PaneLeaf)((PaneSplit)root).Second).Id;

        var result = WorkspaceRules.CloseTab(root, secondLeafId, "customers");

        var remainingLeaf = Assert.IsType<PaneLeaf>(result);
        Assert.Equal(["dashboard"], remainingLeaf.ModuleIds);
    }

    [Fact]
    public void CloseModuleEverywhere_RemovesFromEveryLeafItAppearsIn()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "dashboard", PaneOrientation.Horizontal, NewId);

        var result = WorkspaceRules.CloseModuleEverywhere(root, "dashboard");

        Assert.Null(result);
    }

    [Fact]
    public void Resize_MatchingSplitId_AppliesClampedRatio()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);
        var splitId = ((PaneSplit)root).Id;

        var result = (PaneSplit)WorkspaceRules.Resize(root, splitId, 0.05);

        Assert.Equal(WorkspaceRules.MinRatio, result.Ratio);
    }

    [Fact]
    public void Resize_NonMatchingSplitId_LeavesTreeUnchanged()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);

        var result = (PaneSplit)WorkspaceRules.Resize(root, "not-a-real-split-id", 0.9);

        Assert.Equal(WorkspaceRules.DefaultRatio, result.Ratio);
    }

    [Fact]
    public void FindLeaf_NullRoot_ReturnsNull() =>
        Assert.Null(WorkspaceRules.FindLeaf(null, "leaf-1"));

    [Fact]
    public void FindLeaf_ExistsNestedInsideASplit_FindsIt()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);
        var secondLeafId = ((PaneLeaf)((PaneSplit)root).Second).Id;

        var found = WorkspaceRules.FindLeaf(root, secondLeafId);

        Assert.NotNull(found);
        Assert.Equal(["customers"], found!.ModuleIds);
    }

    [Fact]
    public void AllLeaves_NestedSplits_ReturnsEveryLeafInOrder()
    {
        var root = WorkspaceRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);
        var firstLeafId = ((PaneLeaf)((PaneSplit)root).First).Id;
        root = WorkspaceRules.Split(root, firstLeafId, "dashboard", "bookings", PaneOrientation.Vertical, NewId);

        var leaves = WorkspaceRules.AllLeaves(root).ToList();

        Assert.Equal(3, leaves.Count);
        Assert.Equal(["dashboard"], leaves[0].ModuleIds);
        Assert.Equal(["bookings"], leaves[1].ModuleIds);
        Assert.Equal(["customers"], leaves[2].ModuleIds);
    }
}
