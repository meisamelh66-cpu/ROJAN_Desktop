using Rojan.Desktop.Application.Workspaces;

namespace Rojan.Desktop.Application.Tests.Workspaces;

/// <summary>Exercises <see cref="PaneTreeRules"/> - the pure, in-memory DTO-facing wrapper around <c>Domain.Workspaces.WorkspaceRules</c> that Presentation calls directly for instant pane operations. A thin sanity pass (the underlying algorithm is already covered by <c>Domain.Tests.Workspaces.WorkspaceRulesTests</c>) confirming the DTO round trip itself is correct.</summary>
public sealed class PaneTreeRulesTests
{
    private static int _idCounter;

    private static string NewId() => $"id-{Interlocked.Increment(ref _idCounter)}";

    [Fact]
    public void Split_FromNullRoot_ProducesEquivalentDtoTree()
    {
        var result = PaneTreeRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);

        var split = Assert.IsType<PaneSplitDto>(result);
        Assert.Equal(PaneOrientation.Horizontal, split.Orientation);
        Assert.Equal(["dashboard"], ((PaneLeafDto)split.First).ModuleIds);
        Assert.Equal(["customers"], ((PaneLeafDto)split.Second).ModuleIds);
    }

    [Fact]
    public void OpenTab_ThenCloseTab_RoundTripsBackToTheOriginalLeaf()
    {
        PaneNodeDto leaf = new PaneLeafDto("leaf-1", ["dashboard"], "dashboard");

        var opened = PaneTreeRules.OpenTab(leaf, "leaf-1", "customers");
        var closed = (PaneLeafDto)PaneTreeRules.CloseTab(opened, "leaf-1", "customers")!;

        Assert.Equal(["dashboard"], closed.ModuleIds);
        Assert.Equal("dashboard", closed.ActiveModuleId);
    }

    [Fact]
    public void CloseTab_LastTabInOnlyLeaf_ReturnsNull()
    {
        PaneNodeDto leaf = new PaneLeafDto("leaf-1", ["dashboard"], "dashboard");

        var result = PaneTreeRules.CloseTab(leaf, "leaf-1", "dashboard");

        Assert.Null(result);
    }

    [Fact]
    public void Resize_AppliesClampedRatioToTheMatchingSplit()
    {
        var split = PaneTreeRules.Split(null, null, "dashboard", "customers", PaneOrientation.Horizontal, NewId);
        var splitId = ((PaneSplitDto)split).Id;

        var result = (PaneSplitDto)PaneTreeRules.Resize(split, splitId, 2.0);

        Assert.Equal(PaneTreeRules.ClampRatio(2.0), result.Ratio);
    }
}
