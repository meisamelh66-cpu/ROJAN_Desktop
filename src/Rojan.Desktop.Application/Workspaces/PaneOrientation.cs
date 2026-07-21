namespace Rojan.Desktop.Application.Workspaces;

/// <summary>Application's own mirror of <c>Domain.Workspaces.PaneOrientation</c> - the same "Application owns a parallel copy so Presentation never needs a Domain reference" pattern <c>Notifications.NotificationSeverity</c>/<c>Search.HighlightSpan</c> already establish, enforced by the unchanged <c>ArchitectureTests.DependencyDirectionTests</c>.</summary>
public enum PaneOrientation
{
    Horizontal,
    Vertical,
}
