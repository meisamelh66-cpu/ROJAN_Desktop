namespace Rojan.Desktop.Application.Workspaces;

/// <summary>Application's own mirror of <c>Domain.Workspaces.WorkspaceLayout</c> - see <see cref="PaneOrientation"/>'s doc comment for why.</summary>
public sealed record WorkspaceLayoutDto(
    string Id,
    string Name,
    string PrimaryModuleId,
    PaneNodeDto? SecondaryRoot,
    IReadOnlyList<DockedPanelDto> DockedPanels,
    IReadOnlyList<FloatingWindowDto> FloatingWindows,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDefault);
