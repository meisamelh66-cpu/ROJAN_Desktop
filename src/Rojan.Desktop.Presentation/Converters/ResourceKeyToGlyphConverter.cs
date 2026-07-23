using System.Globalization;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Phase C-2 (Recent Alerts Panel): resolves an icon resource key (e.g.
/// "Rojan.Icon.Warning") to its glyph string at the View layer - keeps
/// ViewModels.Dashboard.AlertItem a plain data holder (a key name) rather
/// than the ViewModel calling System.Windows.Application.Current itself,
/// unlike DashboardPage.xaml.cs's code-behind ResolveIcon helper
/// (View-layer code, not a ViewModel).
/// </summary>
public sealed class ResourceKeyToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key ? System.Windows.Application.Current.TryFindResource(key) as string ?? string.Empty : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("ResourceKeyToGlyphConverter is one-way (display only).");
}
