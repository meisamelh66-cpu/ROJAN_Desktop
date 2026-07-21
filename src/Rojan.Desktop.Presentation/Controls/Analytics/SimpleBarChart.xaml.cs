using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Presentation.Controls.Analytics;

/// <summary>
/// Native rendering of a <see cref="ChartDefinitionDto"/> - this app's
/// entire "chart architecture" requirement (Phase 20 explicitly rules out
/// any external charting library, extended by Phase 33's own "native
/// WPF only" chart-engine scope). Renders the chart's first series as:
/// proportional horizontal bars for Bar/Column/HeatMap (HeatMap has no
/// dedicated visual yet - architecture-ready only, per
/// <c>Domain.Reporting.ChartType.HeatMap</c>'s own doc comment), real pie
/// wedges (native <see cref="Path"/>/<see cref="ArcSegment"/>) for
/// Pie/Donut, and a real <see cref="Polyline"/> for Line/Area/Trend.
/// </summary>
public partial class SimpleBarChart : UserControl
{
    public static readonly DependencyProperty ChartProperty =
        DependencyProperty.Register(nameof(Chart), typeof(ChartDefinitionDto), typeof(SimpleBarChart), new PropertyMetadata(null, OnChartChanged));

    private const double MaxBarWidth = 220;
    private const double PieDiameter = 180;
    private const double PieRadius = PieDiameter / 2;

    private static readonly string[] PaletteBrushKeys =
    [
        "Rojan.Brush.Accent",
        "Rojan.Brush.AccentAqua",
        "Rojan.Brush.AccentRoseGold",
        "Rojan.Brush.AccentLavender",
        "Rojan.Brush.Success",
        "Rojan.Brush.Warning",
    ];

    private readonly ObservableCollection<ChartBarItem> _bars = [];
    private readonly ObservableCollection<ChartBarItem> _pieLegend = [];

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
        // INotifyCollectionChanged.
        BarsItemsControl.ItemsSource = _bars;
        PieLegendItemsControl.ItemsSource = _pieLegend;
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
        _pieLegend.Clear();
        PieCanvas.Children.Clear();
        LineCanvas.Children.Clear();
        BarsItemsControl.Visibility = Visibility.Collapsed;
        PieCanvas.Visibility = Visibility.Collapsed;
        PieLegendItemsControl.Visibility = Visibility.Collapsed;
        LineCanvas.Visibility = Visibility.Collapsed;

        var chart = Chart;
        var series = chart is { Series.Count: > 0 } ? chart.Series[0] : null;
        if (chart is null || series is null || series.Values.Count == 0)
        {
            BarsItemsControl.Visibility = Visibility.Visible;
            return;
        }

        switch (chart.ChartType)
        {
            case ChartType.Pie:
            case ChartType.Donut:
                RebuildPie(chart, series);
                break;
            case ChartType.Line:
            case ChartType.Area:
            case ChartType.Trend:
                RebuildLine(chart, series);
                break;
            default:
                RebuildBars(chart, series);
                break;
        }
    }

    private void RebuildBars(ChartDefinitionDto chart, ChartSeriesDto series)
    {
        BarsItemsControl.Visibility = Visibility.Visible;
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

    private void RebuildPie(ChartDefinitionDto chart, ChartSeriesDto series)
    {
        PieCanvas.Visibility = Visibility.Visible;
        PieLegendItemsControl.Visibility = Visibility.Visible;

        var total = series.Values.Sum();
        if (total <= 0m)
        {
            return;
        }

        var isDonut = chart.ChartType == ChartType.Donut;
        var innerRadius = isDonut ? PieRadius * 0.55 : 0d;
        var center = new Point(PieRadius, PieRadius);
        var startAngle = 0d;

        for (var i = 0; i < chart.Categories.Count && i < series.Values.Count; i++)
        {
            var value = series.Values[i];
            var sweepAngle = (double)(value / total) * 360d;
            var brush = ResolvePaletteBrush(i);

            PieCanvas.Children.Add(BuildWedge(center, PieRadius, innerRadius, startAngle, sweepAngle, brush));

            _pieLegend.Add(new ChartBarItem
            {
                Label = chart.Categories[i],
                DisplayValue = value.ToString("N0", CultureInfo.InvariantCulture),
                BarWidth = 0,
                SliceBrush = brush,
            });

            startAngle += sweepAngle;
        }
    }

    private static Path BuildWedge(Point center, double radius, double innerRadius, double startAngleDegrees, double sweepAngleDegrees, Brush brush)
    {
        // A full-circle single-category chart (sweep >= 360) has no valid
        // start/end arc points - draw a filled circle/ring directly instead
        // of building a degenerate ArcSegment.
        if (sweepAngleDegrees >= 359.999)
        {
            return innerRadius > 0
                ? new Path { Data = BuildRingGeometry(center, radius, innerRadius), Fill = brush }
                : new Path { Data = new EllipseGeometry(center, radius, radius), Fill = brush };
        }

        var startPoint = PointOnCircle(center, radius, startAngleDegrees);
        var endPoint = PointOnCircle(center, radius, startAngleDegrees + sweepAngleDegrees);
        var isLargeArc = sweepAngleDegrees > 180d;

        var figure = new PathFigure { IsClosed = true };
        if (innerRadius > 0)
        {
            var innerEndPoint = PointOnCircle(center, innerRadius, startAngleDegrees + sweepAngleDegrees);
            var innerStartPoint = PointOnCircle(center, innerRadius, startAngleDegrees);
            figure.StartPoint = startPoint;
            figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(innerEndPoint, true));
            figure.Segments.Add(new ArcSegment(innerStartPoint, new Size(innerRadius, innerRadius), 0, isLargeArc, SweepDirection.Counterclockwise, true));
        }
        else
        {
            figure.StartPoint = center;
            figure.Segments.Add(new LineSegment(startPoint, true));
            figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true));
        }

        return new Path { Data = new PathGeometry([figure]), Fill = brush };
    }

    private static CombinedGeometry BuildRingGeometry(Point center, double outerRadius, double innerRadius)
    {
        var outer = new EllipseGeometry(center, outerRadius, outerRadius);
        var inner = new EllipseGeometry(center, innerRadius, innerRadius);
        return new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner);
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var radians = (Math.PI / 180d) * (angleDegrees - 90d);
        return new Point(center.X + (radius * Math.Cos(radians)), center.Y + (radius * Math.Sin(radians)));
    }

    private void RebuildLine(ChartDefinitionDto chart, ChartSeriesDto series)
    {
        LineCanvas.Visibility = Visibility.Visible;
        if (chart.Categories.Count == 0 || series.Values.Count == 0)
        {
            return;
        }

        const double width = 320;
        const double height = 120;
        const double padding = 8;

        var max = series.Values.Max();
        var min = Math.Min(0m, series.Values.Min());
        var range = max - min == 0 ? 1m : max - min;
        var pointCount = Math.Min(chart.Categories.Count, series.Values.Count);
        var stepX = pointCount <= 1 ? 0d : (width - (2 * padding)) / (pointCount - 1);

        var points = new PointCollection();
        for (var i = 0; i < pointCount; i++)
        {
            var x = padding + (i * stepX);
            var normalized = (double)((series.Values[i] - min) / range);
            var y = padding + ((1 - normalized) * (height - (2 * padding)));
            points.Add(new Point(x, y));
        }

        var accentBrush = ResolvePaletteBrush(0);

        if (chart.ChartType == ChartType.Area && points.Count > 0)
        {
            var areaPoints = new PointCollection(points) { new Point(points[^1].X, height - padding), new Point(points[0].X, height - padding) };
            LineCanvas.Children.Add(new Polygon { Points = areaPoints, Fill = accentBrush, Opacity = 0.25 });
        }

        LineCanvas.Children.Add(new Polyline { Points = points, Stroke = accentBrush, StrokeThickness = 2.5 });

        foreach (var point in points)
        {
            var dot = new Ellipse { Width = 6, Height = 6, Fill = accentBrush };
            Canvas.SetLeft(dot, point.X - 3);
            Canvas.SetTop(dot, point.Y - 3);
            LineCanvas.Children.Add(dot);
        }
    }

    private Brush ResolvePaletteBrush(int index)
    {
        var key = PaletteBrushKeys[index % PaletteBrushKeys.Length];
        return TryFindResource(key) as Brush ?? Brushes.Gray;
    }
}
