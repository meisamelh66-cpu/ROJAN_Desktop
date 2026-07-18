namespace Rojan.Desktop.Shell.Navigation;

/// <summary>
/// The application's known top-level sections, for the sidebar menu.
/// No page content exists for any of these yet - each becomes a real
/// destination (via <see cref="Presentation.Navigation.INavigationService"/>)
/// once its feature phase implements a ViewModel/View pair.
/// </summary>
public enum ShellSection
{
    Dashboard,
    Crm,
    Booking,
    AiAssistant,
    Reports,
    Settings,
}
