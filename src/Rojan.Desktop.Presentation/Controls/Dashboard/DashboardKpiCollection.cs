using System.Collections;
using System.Collections.Specialized;
using System.Linq;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Bug fix: a live, read-only view over the real KpiMetrics collection plus
/// a fixed set of appended synthetic items (the two analytics cards) - re-
/// raises the source collection's CollectionChanged as a Reset so an
/// ItemsControl bound to this view stays correctly reactive to
/// DashboardPageViewModel's Clear()/Add() calls in LoadAsync.
///
/// This exists because a plain value-converter snapshot
/// (source.Concat(extras).ToList()) only reflects whatever KpiMetrics
/// contained the one time the binding first evaluates - typically empty,
/// since LoadAsync populates it asynchronously afterwards - and a plain
/// List doesn't implement INotifyCollectionChanged, so the ItemsControl
/// would never see the real data arrive. Wrapping instead of snapshotting
/// keeps the same live-update behavior the original
/// ItemsSource="{Binding KpiMetrics}" binding always had.
/// </summary>
public sealed class DashboardKpiCollection : IEnumerable<object>, INotifyCollectionChanged
{
    private readonly IEnumerable _source;
    private readonly IReadOnlyList<object> _appended;
    private readonly Func<object, int>? _sourceSortKeySelector;

    public DashboardKpiCollection(IEnumerable source, IReadOnlyList<object> appended, Func<object, int>? sourceSortKeySelector = null)
    {
        _source = source;
        _appended = appended;
        _sourceSortKeySelector = sourceSortKeySelector;
        if (source is INotifyCollectionChanged notifier)
        {
            notifier.CollectionChanged += OnSourceCollectionChanged;
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public IEnumerator<object> GetEnumerator()
    {
        // Phase B-2 (KPI reference-parity reorder): re-sorts _source fresh on
        // every enumeration (never a cached/materialized snapshot), so this
        // stays exactly as live as the plain foreach it replaces - the
        // ItemsControl's Reset-driven re-enumeration (see
        // OnSourceCollectionChanged) still picks up whatever KpiMetrics
        // currently contains, just in reference order instead of repository
        // order.
        var sourceItems = _source.Cast<object>();
        if (_sourceSortKeySelector is not null)
        {
            sourceItems = sourceItems.OrderBy(_sourceSortKeySelector);
        }

        foreach (var item in sourceItems)
        {
            yield return item;
        }

        foreach (var item in _appended)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
}
