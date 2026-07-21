namespace Rojan.Desktop.Application.Workspaces;

/// <summary>Application's own mirror of <c>Domain.Workspaces.DockedPanelState</c> - see <see cref="PaneOrientation"/>'s doc comment for why.</summary>
public sealed record DockedPanelDto(string PanelKey, DockSide Side, double Size, bool IsVisible);
