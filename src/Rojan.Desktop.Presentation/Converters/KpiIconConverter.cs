using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Dashboard;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Maps a <see cref="KpiMetricDto"/>'s stable <c>Id</c> (e.g. "kpi-bookings")
/// to one of the app's existing Segoe Fluent Icons glyph tokens (see
/// Themes/Icons.xaml), so every KPI card gets a real, deterministic icon
/// instead of a generic placeholder. Falls back to the generic Dashboard
/// glyph for any future/unrecognized id rather than throwing.
/// </summary>
public sealed class KpiIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var id = value as string;

        var key = id switch
        {
            "kpi-bookings" => "Rojan.Icon.Bookings",
            "kpi-clients" => "Rojan.Icon.Customers",
            "kpi-revenue" => "Rojan.Icon.Accounting",
            "kpi-tasks" => "Rojan.Icon.CheckCircle",
            _ => "Rojan.Icon.Dashboard",
        };

        return System.Windows.Application.Current.TryFindResource(key) as string ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("KpiIconConverter is one-way (display only).");
}
