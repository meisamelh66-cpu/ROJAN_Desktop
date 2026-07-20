using System.Windows;
using Rojan.Desktop.Presentation.Theming;

namespace Rojan.Desktop.Shell.Theming;

/// <summary>
/// Assembles the whole design system into <c>Application.Resources</c>
/// at startup, choosing Light.xaml or Dark.xaml per <see cref="IThemeService.ResolvedTheme"/> -
/// the one place, app-wide, that decides which theme file is active (see
/// App.xaml's own doc comment for why this moved out of static XAML).
/// Merge order matters (StaticResource lookups inside a later dictionary
/// need an earlier one's keys already in scope) and mirrors exactly what
/// the old Themes/RojanTheme.xaml merged, minus the redundant self-merges
/// Controls.xaml/ShellChrome.xaml/RojanTheme.xaml used to carry for
/// standalone use - now that every consumer (Application.Resources plus
/// every View, which no longer merges its own copy) shares this single
/// tree, that redundancy was removed rather than fought.
/// </summary>
internal static class ThemeResources
{
    private const string PackPrefix = "pack://application:,,,/Rojan.Desktop.Presentation;component/Themes/";

    private static readonly string[] ThemeInvariantFiles =
    [
        "Colors.xaml",
    ];

    private static readonly string[] RestOfDesignSystem =
    [
        "Typography.xaml",
        "Spacing.xaml",
        "Shapes.xaml",
        "Icons.xaml",
        "Shadows.xaml",
        "Elevation.xaml",
        "Controls.xaml",
        "ShellChrome.xaml",
        "DashboardComponents.xaml",
        "Views.xaml",
    ];

    /// <summary>Merges Colors.xaml, then the resolved theme's own color file, then the rest of the (now theme-agnostic) design system, into <paramref name="application"/>'s resources - in that exact order.</summary>
    public static void Apply(System.Windows.Application application, ThemeMode resolvedTheme)
    {
        var themeFile = resolvedTheme == ThemeMode.Dark ? "Dark.xaml" : "Light.xaml";

        foreach (var file in ThemeInvariantFiles)
        {
            Merge(application, file);
        }

        Merge(application, themeFile);

        foreach (var file in RestOfDesignSystem)
        {
            Merge(application, file);
        }
    }

    private static void Merge(System.Windows.Application application, string fileName) =>
        application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(PackPrefix + fileName, UriKind.Absolute) });
}
