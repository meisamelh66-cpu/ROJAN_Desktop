using DomainWorkspaces = Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Infrastructure.Workspaces;

/// <summary>JSON-serializable mirror of <c>Domain.Workspaces.WorkspaceLayout</c> - see <see cref="PaneNodeRecord"/>'s doc comment for why.</summary>
internal sealed record WorkspaceLayoutRecord(
    string Id,
    string Name,
    string PrimaryModuleId,
    PaneNodeRecord? SecondaryRoot,
    IReadOnlyList<DomainWorkspaces.DockedPanelState> DockedPanels,
    IReadOnlyList<DomainWorkspaces.FloatingWindowState> FloatingWindows,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDefault);

/// <summary>JSON-serializable persisted state that isn't part of any one workspace: which workspace is currently active, and the most-recently-switched-to ids.</summary>
internal sealed record WorkspaceStateRecord(string? ActiveWorkspaceId, List<string> RecentWorkspaceIds);
