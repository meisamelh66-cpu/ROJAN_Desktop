using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Collapses a section whose backing list/string is empty (e.g. the Context Help Dialog's optional Tips/Warnings/Best Practices sections) rather than showing an empty header.</summary>
public sealed class CollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        string text => text.Length == 0 ? Visibility.Collapsed : Visibility.Visible,
        ICollection collection => collection.Count == 0 ? Visibility.Collapsed : Visibility.Visible,
        null => Visibility.Collapsed,
        _ => Visibility.Visible,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("CollectionToVisibilityConverter is one-way (display only).");
}
