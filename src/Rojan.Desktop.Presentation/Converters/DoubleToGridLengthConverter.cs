using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Pixel-mode <see cref="GridLength"/> from a plain <see cref="double"/> - used to bind a docked panel's <c>ColumnDefinition</c>/<c>RowDefinition</c> size directly to its ViewModel (0 collapses it to no width/height at all, no separate visibility toggle needed).</summary>
public sealed class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new GridLength(value is double d and > 0 ? d : 0, GridUnitType.Pixel);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is GridLength length ? length.Value : 0d;
}
