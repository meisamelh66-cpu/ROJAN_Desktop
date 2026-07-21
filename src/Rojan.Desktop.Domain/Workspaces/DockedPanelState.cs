namespace Rojan.Desktop.Domain.Workspaces;

/// <summary>A pinned side panel's arrangement within a workspace, as returned by <see cref="IWorkspaceRepository"/>. <see cref="PanelKey"/> is a free-form identifier a Presentation-layer registry maps to actual panel content - Domain has no knowledge of what a panel displays.</summary>
public sealed record DockedPanelState(string PanelKey, DockSide Side, double Size, bool IsVisible);
