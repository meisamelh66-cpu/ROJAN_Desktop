using DomainWorkspaces = Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Application.Workspaces;

/// <summary>
/// Pure, synchronous, no-I/O wrapper around <c>Domain.Workspaces.WorkspaceRules</c>
/// for callers that only see this layer's mirrored <see cref="PaneNodeDto"/>
/// tree - i.e. Presentation, which cannot reference Domain directly (see
/// <c>ArchitectureTests.DependencyDirectionTests</c>). Maps to Domain's
/// <c>PaneNode</c> via <see cref="WorkspaceMapping"/>, delegates to the
/// canonical rule, maps the result back. This is what lets interactive pane
/// operations (split/open tab/close tab/resize) stay instant and entirely
/// in-memory in <c>WorkspaceHostViewModel</c> - persisting the result is a
/// deliberately separate, debounced <see cref="IWorkspaceService.SaveLayoutAsync"/>
/// call the caller makes afterward, not a round trip on every click/drag.
/// </summary>
public static class PaneTreeRules
{
    public static double ClampRatio(double ratio) => DomainWorkspaces.WorkspaceRules.ClampRatio(ratio);

    public static double ClampDockSize(double size) => DomainWorkspaces.WorkspaceRules.ClampDockSize(size);

    public static PaneNodeDto Split(PaneNodeDto? root, string? targetLeafId, string primaryModuleId, string newModuleId, PaneOrientation orientation, Func<string> newId) =>
        WorkspaceMapping.Map(DomainWorkspaces.WorkspaceRules.Split(
            WorkspaceMapping.MapToDomain(root), targetLeafId, primaryModuleId, newModuleId, WorkspaceMapping.MapToDomain(orientation), newId))!;

    public static PaneNodeDto OpenTab(PaneNodeDto root, string leafId, string moduleId) =>
        WorkspaceMapping.Map(DomainWorkspaces.WorkspaceRules.OpenTab(WorkspaceMapping.MapToDomain(root)!, leafId, moduleId))!;

    public static PaneNodeDto SetActiveTab(PaneNodeDto root, string leafId, string moduleId) =>
        WorkspaceMapping.Map(DomainWorkspaces.WorkspaceRules.SetActiveTab(WorkspaceMapping.MapToDomain(root)!, leafId, moduleId))!;

    public static PaneNodeDto? CloseTab(PaneNodeDto root, string leafId, string moduleId) =>
        WorkspaceMapping.Map(DomainWorkspaces.WorkspaceRules.CloseTab(WorkspaceMapping.MapToDomain(root)!, leafId, moduleId));

    public static PaneNodeDto? CloseModuleEverywhere(PaneNodeDto root, string moduleId) =>
        WorkspaceMapping.Map(DomainWorkspaces.WorkspaceRules.CloseModuleEverywhere(WorkspaceMapping.MapToDomain(root)!, moduleId));

    public static PaneNodeDto Resize(PaneNodeDto node, string splitId, double ratio) =>
        WorkspaceMapping.Map(DomainWorkspaces.WorkspaceRules.Resize(WorkspaceMapping.MapToDomain(node)!, splitId, ratio))!;
}
