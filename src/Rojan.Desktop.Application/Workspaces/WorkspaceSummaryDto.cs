namespace Rojan.Desktop.Application.Workspaces;

/// <summary>Lightweight listing shape for the Workspace Switcher - everything it needs to show a row without loading the full pane tree.</summary>
public sealed record WorkspaceSummaryDto(string Id, string Name, bool IsActive, bool IsDefault, DateTimeOffset UpdatedAt);
