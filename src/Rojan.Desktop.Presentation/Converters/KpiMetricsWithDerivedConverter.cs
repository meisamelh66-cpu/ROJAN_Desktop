using System.Collections;
using System.Globalization;
using System.Windows.Data;
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
    private static readonly DerivedKpiSpec[] DerivedCards =
    [
        new() { Id = "derived-avg-transaction" },
        new() { Id = "derived-avg-bookings-per-client" },
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IEnumerable source ? new DashboardKpiCollection(source, DerivedCards) : DerivedCards;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("KpiMetricsWithDerivedConverter is one-way (display only).");
}
