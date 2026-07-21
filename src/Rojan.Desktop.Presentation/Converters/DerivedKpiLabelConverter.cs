using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>Phase 35: maps a synthetic derived-KPI Id to its localized display label - the DerivedKpiSpec analog of KpiLabelConverter.</summary>
public sealed class DerivedKpiLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        "derived-avg-transaction" => Strings.Dashboard_Kpi_AvgTransactionValue,
        "derived-avg-bookings-per-client" => Strings.Dashboard_Kpi_AvgBookingsPerClient,
        _ => string.Empty,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("DerivedKpiLabelConverter is one-way (display only).");
}
