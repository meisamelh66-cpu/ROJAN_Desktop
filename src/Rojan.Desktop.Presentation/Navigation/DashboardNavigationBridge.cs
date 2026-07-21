namespace Rojan.Desktop.Presentation.Navigation;

/// <summary>
/// Bug fix: DashboardPage's Quick Action buttons need to trigger real
/// navigation, but the View's code-behind has no constructor-injection
/// point the way ViewModels do (<see cref="INavigationService"/> is only
/// ever handed to ViewModels via DI) - and DashboardPageViewModel itself is
/// off-limits ("Do NOT modify ViewModels"). Shell sets this once, right next
/// to where it already attaches the same NavigationService instance to the
/// window's content host (see MainWindow.xaml.cs) - a single assignment,
/// not a restructuring of navigation or DI. Deliberately scoped to exactly
/// this one need rather than a general-purpose service locator.
/// </summary>
public static class DashboardNavigationBridge
{
    public static INavigationService? Current { get; set; }
}
