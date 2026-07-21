namespace Rojan.Desktop.Application.Workspaces;

/// <summary>Application's own mirror of <c>Domain.Workspaces.PaneNode</c> - see <see cref="PaneOrientation"/>'s doc comment for why. <see cref="WorkspaceService"/> maps between the two at the Application/Domain boundary.</summary>
public abstract record PaneNodeDto(string Id);

public sealed record PaneLeafDto(string Id, IReadOnlyList<string> ModuleIds, string? ActiveModuleId) : PaneNodeDto(Id);

public sealed record PaneSplitDto(string Id, PaneOrientation Orientation, double Ratio, PaneNodeDto First, PaneNodeDto Second) : PaneNodeDto(Id);
