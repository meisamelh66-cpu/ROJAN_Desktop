using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Maps a <see cref="KpiMetricDto"/>'s stable <c>Id</c> (e.g.
/// "kpi-bookings") to a localized display label. FakeDashboardRepository
/// (Infrastructure) returns English labels baked into its sample data and
/// cannot depend on Presentation's Strings, so the localization happens
/// here at the View boundary instead - bound against the whole DTO (not
/// just its Label) so an unrecognized id can still fall back to the
/// repository-provided Label.
/// </summary>
public sealed class KpiLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not KpiMetricDto metric)
        {
            return string.Empty;
        }

        return metric.Id switch
        {
            "kpi-bookings" => Strings.Dashboard_Kpi_TotalBookings,
            "kpi-clients" => Strings.Dashboard_Kpi_ActiveClients,
            "kpi-revenue" => Strings.Dashboard_Kpi_RevenueMtd,
            "kpi-tasks" => Strings.Dashboard_Kpi_PendingTasks,
            _ => metric.Label,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("KpiLabelConverter is one-way (display only).");
}
