namespace Rojan.Desktop.Application.Workspaces;

/// <summary>Application's own mirror of <c>Domain.Workspaces.FloatingWindowState</c> - see <see cref="PaneOrientation"/>'s doc comment for why.</summary>
public sealed record FloatingWindowDto(string Id, string ModuleId, double X, double Y, double Width, double Height, bool IsMaximized);
