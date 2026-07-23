using System.Collections;
using System.Globalization;
using System.Windows.Data;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.Controls.Dashboard;

namespace Rojan.Desktop.Presentation.Converters;

/// <summary>
/// Bug fix (Phase 35 follow-up): the KPI ItemsControl previously fed a
/// CompositeCollection whose <c>CollectionContainer Collection="{Binding
/// KpiMetrics}"</c> silently failed to resolve - CollectionContainer isn't a
/// FrameworkElement, so it never inherits DataContext, meaning the real four
/// KpiMetrics never actually joined the collection and only the two
/// synthetic analytics cards rendered.
///
/// This converter wraps the real, live KpiMetrics collection (never a
/// disconnected snapshot - see DashboardKpiCollectionView) with the two
/// synthetic analytics markers appended, bound as an ordinary
/// <c>ItemsSource="{Binding KpiMetrics, Converter=...}"</c> - the
/// ItemsControl is a real FrameworkElement with normal DataContext
/// inheritance, so this always sees the real collection, and the returned
/// view stays reactive to it afterwards.
/// </summary>
public sealed class KpiMetricsWithDerivedConverter : IValueConverter
{
    /// <summary>
    /// Phase B-2 (KPI reference-parity reduction): reference shows 5 KPI
    /// cards, not 6 - dropping one of the two Phase 35 synthetic analytics
    /// cards (Avg Bookings/Client) rather than a real metric, since the
    /// real four are the app's actual tracked data and the synthetic cards
    /// exist only to fill out the row.
    /// </summary>
    private static readonly DerivedKpiSpec[] DerivedCards =
    [
        new() { Id = "derived-avg-transaction" },
    ];

    /// <summary>
    /// Phase B-2: reference-parity card order (Revenue, Bookings, Active
    /// Clients, Pending Tasks) - the repository's seed order (bookings,
    /// clients, revenue, tasks) has no presentation significance of its own,
    /// so reordering here (a display concern) rather than in
    /// FakeDashboardRepository (a data concern) keeps this out of
    /// Infrastructure. Unknown ids sort last (Count as a safe fallback) so a
    /// future real metric added upstream still renders instead of vanishing.
    /// </summary>
    private static readonly string[] ReferenceOrder =
    [
        "kpi-revenue",
        "kpi-bookings",
        "kpi-clients",
        "kpi-tasks",
    ];

    private static int SourceSortKey(object item)
    {
        if (item is not KpiMetricDto metric)
        {
            return ReferenceOrder.Length;
        }

        var index = Array.IndexOf(ReferenceOrder, metric.Id);
        return index < 0 ? ReferenceOrder.Length : index;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IEnumerable source ? new DashboardKpiCollection(source, DerivedCards, SourceSortKey) : DerivedCards;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("KpiMetricsWithDerivedConverter is one-way (display only).");
}
