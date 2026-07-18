namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>Load state for a dashboard section - drives which visual DashboardWidget shows.</summary>
public enum DashboardState
{
    Loading,
    Loaded,
    Empty,
    Error,
}
