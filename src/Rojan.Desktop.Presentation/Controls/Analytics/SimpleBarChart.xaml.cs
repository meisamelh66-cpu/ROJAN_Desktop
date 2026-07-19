using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Presentation.Controls.Analytics;

/// <summary>
/// Native, proportional-bar rendering of a <see cref="ChartDefinitionDto"/> -
/// this app's entire "chart architecture and placeholder rendering"
/// requirement (Phase 20 explicitly rules out any external charting
/// library). Renders the chart's first series as horizontal bars sized
/// relative to the series' own maximum value; used for every
/// <see cref="ChartType"/> (Line/Bar/Pie/Area/Column all render the same
/// way here - the point is proving the data model end-to-end, not a
/// bespoke visual per chart type).
/// </summary>
public partial class SimpleBarChart : UserControl
{
    public static readonly DependencyProperty ChartProperty =
        DependencyProperty.Register(nameof(Chart), typeof(ChartDefinitionDto), typeof(SimpleBarChart), new PropertyMetadata(null, OnChartChanged));

    private const double MaxBarWidth = 220;

    private readonly ObservableCollection<ChartBarItem> _bars = [];

    public SimpleBarChart()
    {
        InitializeComponent();

        // Assigned directly, not via "{Binding Bars, ElementName=Root}" -
        // proved unreliable once this control is itself hosted inside an
        // outer ItemsControl's own DataTemplate (AnalyticsPage's Chart
        // Area): Rebuild() populated the collection correctly every time,
        // but bars never rendered. Setting ItemsSource once here sidesteps
        // ElementName resolution entirely - WPF still tracks future
        // Add/Clear calls via the ObservableCollection's own
        // INotifyCollectionChanged, so Rebuild() mutating _bars in place
        // still updates the UI.
        BarsItemsControl.ItemsSource = _bars;
    }

    public ChartDefinitionDto? Chart
    {
        get => (ChartDefinitionDto?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    private static void OnChartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SimpleBarChart control)
        {
            control.Rebuild();
        }
    }

    private void Rebuild()
    {
        _bars.Clear();
        var chart = Chart;
        var series = chart is { Series.Count: > 0 } ? chart.Series[0] : null;
        if (chart is null || series is null || series.Values.Count == 0)
        {
            return;
        }

        var max = series.Values.Count == 0 ? 0m : series.Values.Max();
        for (var i = 0; i < chart.Categories.Count && i < series.Values.Count; i++)
        {
            var value = series.Values[i];
            var width = max <= 0m ? 0d : (double)(value / max) * MaxBarWidth;
            _bars.Add(new ChartBarItem
            {
                Label = chart.Categories[i],
                DisplayValue = value.ToString("N0", CultureInfo.InvariantCulture),
                BarWidth = Math.Max(2d, width),
            });
        }
    }
}
