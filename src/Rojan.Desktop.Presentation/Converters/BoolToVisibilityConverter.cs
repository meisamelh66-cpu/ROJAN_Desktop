using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Bool-to-<see cref="Visibility"/> with an optional invert, for the
/// Context Help Dialog's mutually-exclusive "search results vs. topic
/// content" panes (<c>ConverterParameter="Invert"</c> for the pane that
/// shows only while search is inactive) - the WPF-supplied
/// <see cref="System.Windows.Controls.BooleanToVisibilityConverter"/> has
/// no invert option.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("BoolToVisibilityConverter is one-way (display only).");
}
