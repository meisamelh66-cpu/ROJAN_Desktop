using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Phase 26.7: joins the Context Help Dialog's breadcrumb trail (e.g. "Help Home", "Customers") into one "Help Home › Customers" display string.</summary>
public sealed class BreadcrumbJoinConverter : IValueConverter
{
    private const string Separator = " › ";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IEnumerable items and not string
            ? string.Join(Separator, items.Cast<object>())
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("BreadcrumbJoinConverter is one-way (display only).");
}
