namespace Rojan.Desktop.Domain.Workspaces;

/// <summary>
/// A complete, nameable, saveable arrangement of the workspace - which
/// module the sidebar-driven primary pane shows, what secondary panes/tabs
/// are split alongside it, which panels are docked, and which modules are
/// floating in their own windows. <see cref="PrimaryModuleId"/> is
/// deliberately separate from <see cref="SecondaryRoot"/>: the primary pane
/// is the pre-existing, always-present, sidebar-driven content region
/// (unchanged since Phase 07) - <see cref="SecondaryRoot"/> being
/// <see langword="null"/> means "no extra panes," the default single-pane
/// state every workspace starts in.
/// </summary>
public sealed record WorkspaceLayout(
    string Id,
    string Name,
    string PrimaryModuleId,
    PaneNode? SecondaryRoot,
    IReadOnlyList<DockedPanelState> DockedPanels,
    IReadOnlyList<FloatingWindowState> FloatingWindows,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDefault);
