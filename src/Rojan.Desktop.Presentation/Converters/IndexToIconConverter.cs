using System.Globalization;
using System.Windows.Data;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Cycles a small curated set of existing Fluent icon glyph tokens (see
/// Themes/Icons.xaml) by a zero-based index - used for Quick Actions
/// buttons, whose <c>QuickActionItem</c> model carries only a display
/// <c>Label</c>, no per-item icon. Bound to WPF's built-in
/// <c>ItemsControl.AlternationIndex</c> attached property so each button
/// gets a distinct, deterministic (not fabricated) icon without adding any
/// new bindable data to the ViewModel/DTO layer.
/// </summary>
public sealed class IndexToIconConverter : IValueConverter
{
    private static readonly string[] IconKeys =
    [
        "Rojan.Icon.Add",
        "Rojan.Icon.Calendar2",
        "Rojan.Icon.Customers",
        "Rojan.Icon.Bookings",
        "Rojan.Icon.Reports",
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var index = value is int i ? i : 0;
        var key = IconKeys[((index % IconKeys.Length) + IconKeys.Length) % IconKeys.Length];
        return System.Windows.Application.Current.TryFindResource(key) as string ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("IndexToIconConverter is one-way (display only).");
}
