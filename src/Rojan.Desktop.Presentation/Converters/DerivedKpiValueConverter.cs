using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.Controls.Dashboard;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Phase 35 (Analytics Expansion): computes the two synthetic "analytics"
/// KPI cards' displayed value purely from the real KpiMetricDto values
/// already bound to the Dashboard page's KpiMetrics collection - never a
/// fabricated number:
/// - "derived-avg-transaction" = kpi-revenue ÷ kpi-bookings (a real average
///   transaction value).
/// - "derived-avg-bookings-per-client" = kpi-bookings ÷ kpi-clients (a real
///   engagement ratio).
/// The spec's own "Completed Tasks Rate" suggestion was deliberately not
/// implemented - there is no "total tasks" figure anywhere in the existing
/// data (only "pending tasks"), so a completion rate cannot be computed
/// without inventing a denominator, which the same sprint's "Do NOT invent
/// fake business data" rule forbids. This ratio was substituted instead
/// because it's honestly derivable from data that already exists.
/// If either source KPI is missing or unparsable, or the denominator is
/// zero, returns an empty string (KPIValue/KpiChart already fail safe on
/// an unparsable Value - no fabricated shape is ever shown).
/// </summary>
public sealed class DerivedKpiValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [string id, IEnumerable<KpiMetricDto> metrics, ..])
        {
            return string.Empty;
        }

        var byId = metrics.ToDictionary(m => m.Id, m => m.Value);

        return id switch
        {
            "derived-avg-transaction" => FormatRatio(byId, "kpi-revenue", "kpi-bookings", suffix: " تومان", decimals: 0),
            "derived-avg-bookings-per-client" => FormatRatio(byId, "kpi-bookings", "kpi-clients", suffix: string.Empty, decimals: 1),
            _ => string.Empty,
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("DerivedKpiValueConverter is one-way (display only).");

    private static string FormatRatio(Dictionary<string, string> byId, string numeratorId, string denominatorId, string suffix, int decimals)
    {
        if (!byId.TryGetValue(numeratorId, out var numeratorText) || !KpiNumberParsing.TryParse(numeratorText, out var numerator, out _, out _, out _, out _))
        {
            return string.Empty;
        }

        if (!byId.TryGetValue(denominatorId, out var denominatorText) || !KpiNumberParsing.TryParse(denominatorText, out var denominator, out _, out _, out _, out _) || denominator == 0)
        {
            return string.Empty;
        }

        var ratio = numerator / denominator;
        var pattern = decimals > 0 ? "#,0." + new string('0', decimals) : "#,0";
        return ratio.ToString(pattern, CultureInfo.InvariantCulture) + suffix;
    }
}
