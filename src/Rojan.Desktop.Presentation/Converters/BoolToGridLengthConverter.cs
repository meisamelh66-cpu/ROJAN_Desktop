using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// A collapsed (0-width) <see cref="GridLength"/> when <see langword="false"/>,
/// otherwise <see cref="GridLength.Auto"/> or a 1-star <see cref="GridLength"/>
/// depending on <c>ConverterParameter</c> ("Auto"/"Star") - lets a
/// <c>ColumnDefinition</c>/<c>RowDefinition</c> collapse entirely from a
/// plain bound <see cref="bool"/> (e.g. "does the workspace have a
/// secondary pane") with no separate visibility trigger needed.
/// </summary>
public sealed class BoolToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true)
        {
            return new GridLength(0);
        }

        return string.Equals(parameter as string, "Auto", StringComparison.OrdinalIgnoreCase)
            ? GridLength.Auto
            : new GridLength(1, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("BoolToGridLengthConverter is one-way (display only).");
}
