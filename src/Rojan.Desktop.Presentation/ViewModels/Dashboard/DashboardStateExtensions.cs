namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>
/// Page Stability: the one rule every page/profile ViewModel uses to tell an
/// initial load apart from a refresh. Once a load has settled (Loaded, or a
/// genuine Empty), its result is on screen - reloading from there is a
/// refresh: it must keep <see cref="DashboardState"/> where it is (raising an
/// IsRefreshing flag instead of going back to Loading, which DashboardWidget
/// renders by hiding the content), and a failed refresh must surface as an
/// inline ErrorMessage rather than switching to Error, which would replace the
/// still-valid content (and the user's selection/inputs) with a Retry panel.
/// Loading and Error are not settled: a reload from either is still the first
/// successful load, so it keeps the existing full Loading/Error visuals.
/// </summary>
public static class DashboardStateExtensions
{
    public static bool HasSettledResult(this DashboardState state) =>
        state is DashboardState.Loaded or DashboardState.Empty;
}
