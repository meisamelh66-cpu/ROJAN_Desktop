using System.Collections;
using System.Collections.Specialized;

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

    public DashboardKpiCollection(IEnumerable source, IReadOnlyList<object> appended)
    {
        _source = source;
        _appended = appended;
        if (source is INotifyCollectionChanged notifier)
        {
            notifier.CollectionChanged += OnSourceCollectionChanged;
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public IEnumerator<object> GetEnumerator()
    {
        foreach (var item in _source)
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
