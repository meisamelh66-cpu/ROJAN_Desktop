namespace Rojan.Desktop.Presentation.Theming;

/// <summary>
/// The Fluent 2 Premium Theme pass's theme choice - <see cref="Light"/>/
/// <see cref="Dark"/> select a specific token set directly;
/// <see cref="System"/> follows the Windows "AppsUseLightTheme" setting,
/// resolved once at startup (see <c>IThemeService.ResolvedTheme</c>) -
/// same "resolved once, restart to re-resolve" design as the Localization
/// platform's language switch, not a live OS-theme-change listener.
/// </summary>
public enum ThemeMode
{
    Light,
    Dark,
    System,
}
