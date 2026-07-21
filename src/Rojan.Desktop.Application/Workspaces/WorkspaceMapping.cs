using System.Diagnostics.CodeAnalysis;
using DomainWorkspaces = Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Application.Workspaces;

/// <summary>Domain-to-Application (and back) mapping for the Workspaces vertical slice, shared by <see cref="WorkspaceService"/> (persistence) and <see cref="PaneTreeRules"/> (pure in-memory tree operations) so the translation lives in exactly one place.</summary>
internal static class WorkspaceMapping
{
    public static WorkspaceLayoutDto Map(DomainWorkspaces.WorkspaceLayout workspace) => new(
        workspace.Id,
        workspace.Name,
        workspace.PrimaryModuleId,
        Map(workspace.SecondaryRoot),
        workspace.DockedPanels.Select(Map).ToList(),
        workspace.FloatingWindows.Select(Map).ToList(),
        workspace.CreatedAt,
        workspace.UpdatedAt,
        workspace.IsDefault);

    [return: NotNullIfNotNull(nameof(node))]
    public static PaneNodeDto? Map(DomainWorkspaces.PaneNode? node) => node switch
    {
        null => null,
        DomainWorkspaces.PaneLeaf leaf => new PaneLeafDto(leaf.Id, leaf.ModuleIds, leaf.ActiveModuleId),
        DomainWorkspaces.PaneSplit split => new PaneSplitDto(split.Id, Map(split.Orientation), split.Ratio, Map(split.First)!, Map(split.Second)!),
        _ => throw new InvalidOperationException($"Unknown pane node type '{node.GetType()}'."),
    };

    public static DockedPanelDto Map(DomainWorkspaces.DockedPanelState panel) =>
        new(panel.PanelKey, Map(panel.Side), panel.Size, panel.IsVisible);

    public static FloatingWindowDto Map(DomainWorkspaces.FloatingWindowState window) =>
        new(window.Id, window.ModuleId, window.X, window.Y, window.Width, window.Height, window.IsMaximized);

    public static PaneOrientation Map(DomainWorkspaces.PaneOrientation orientation) =>
        orientation == DomainWorkspaces.PaneOrientation.Vertical ? PaneOrientation.Vertical : PaneOrientation.Horizontal;

    public static DockSide Map(DomainWorkspaces.DockSide side) => side switch
    {
        DomainWorkspaces.DockSide.Left => DockSide.Left,
        DomainWorkspaces.DockSide.Right => DockSide.Right,
        _ => DockSide.Bottom,
    };

    public static DomainWorkspaces.WorkspaceLayout MapToDomain(WorkspaceLayoutDto dto) => new(
        dto.Id,
        dto.Name,
        dto.PrimaryModuleId,
        MapToDomain(dto.SecondaryRoot),
        dto.DockedPanels.Select(MapToDomain).ToList(),
        dto.FloatingWindows.Select(MapToDomain).ToList(),
        dto.CreatedAt,
        dto.UpdatedAt,
        dto.IsDefault);

    [return: NotNullIfNotNull(nameof(node))]
    public static DomainWorkspaces.PaneNode? MapToDomain(PaneNodeDto? node) => node switch
    {
        null => null,
        PaneLeafDto leaf => new DomainWorkspaces.PaneLeaf(leaf.Id, leaf.ModuleIds, leaf.ActiveModuleId),
        PaneSplitDto split => new DomainWorkspaces.PaneSplit(split.Id, MapToDomain(split.Orientation), split.Ratio, MapToDomain(split.First)!, MapToDomain(split.Second)!),
        _ => throw new InvalidOperationException($"Unknown pane node dto type '{node.GetType()}'."),
    };

    public static DomainWorkspaces.DockedPanelState MapToDomain(DockedPanelDto panel) =>
        new(panel.PanelKey, MapToDomain(panel.Side), panel.Size, panel.IsVisible);

    public static DomainWorkspaces.FloatingWindowState MapToDomain(FloatingWindowDto window) =>
        new(window.Id, window.ModuleId, window.X, window.Y, window.Width, window.Height, window.IsMaximized);

    public static DomainWorkspaces.PaneOrientation MapToDomain(PaneOrientation orientation) =>
        orientation == PaneOrientation.Vertical ? DomainWorkspaces.PaneOrientation.Vertical : DomainWorkspaces.PaneOrientation.Horizontal;

    public static DomainWorkspaces.DockSide MapToDomain(DockSide side) => side switch
    {
        DockSide.Left => DomainWorkspaces.DockSide.Left,
        DockSide.Right => DomainWorkspaces.DockSide.Right,
        _ => DomainWorkspaces.DockSide.Bottom,
    };
}
