using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// Generic line/area trend chart over a plain list of values - see this
/// file's own XAML doc comment for why it's a new control rather than a
/// moved Controls.Dashboard.KpiChart. Rebuilds its geometry whenever
/// Values is reassigned or the control is resized; does not track
/// in-place mutation of an existing list - consumers replace the whole
/// list on refresh, the same pattern every other chart-shaped binding in
/// this app already uses.
/// </summary>
public partial class MetricChart : UserControl
{
    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(
            nameof(Values),
            typeof(IReadOnlyList<double>),
            typeof(MetricChart),
            new PropertyMetadata(null, OnRebuildTriggeringPropertyChanged));

    public static readonly DependencyProperty LineBrushProperty =
        DependencyProperty.Register(
            nameof(LineBrush),
            typeof(Brush),
            typeof(MetricChart),
            new PropertyMetadata(null, OnRebuildTriggeringPropertyChanged));

    public static readonly DependencyProperty ShowAreaProperty =
        DependencyProperty.Register(
            nameof(ShowArea),
            typeof(bool),
            typeof(MetricChart),
            new PropertyMetadata(true, OnRebuildTriggeringPropertyChanged));

    public MetricChart()
    {
        InitializeComponent();
        PlotArea.SizeChanged += (_, _) => Rebuild();
    }

    public IReadOnlyList<double>? Values
    {
        get => (IReadOnlyList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public Brush? LineBrush
    {
        get => (Brush?)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public bool ShowArea
    {
        get => (bool)GetValue(ShowAreaProperty);
        set => SetValue(ShowAreaProperty, value);
    }

    private static void OnRebuildTriggeringPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((MetricChart)d).Rebuild();

    private void Rebuild()
    {
        var values = Values;
        var width = PlotArea.ActualWidth;
        var height = PlotArea.ActualHeight;

        if (values is null || values.Count < 2 || width <= 0 || height <= 0)
        {
            LinePolyline.Points = null;
            AreaPolygon.Points = null;
            return;
        }

        var min = values.Min();
        var max = values.Max();
        if (max - min < double.Epsilon)
        {
            min -= 1;
            max += 1;
        }

        var points = new PointCollection(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var x = i / (double)(values.Count - 1) * width;
            var y = height - (values[i] - min) / (max - min) * height;
            points.Add(new Point(x, y));
        }

        var brush = LineBrush ?? TryFindResource("Rojan.Brush.Accent") as Brush;
        LinePolyline.Stroke = brush;
        LinePolyline.Points = points;

        if (ShowArea)
        {
            var areaPoints = new PointCollection(points);
            areaPoints.Add(new Point(width, height));
            areaPoints.Add(new Point(0, height));
            AreaPolygon.Fill = brush;
            AreaPolygon.Opacity = 0.16;
            AreaPolygon.Points = areaPoints;
        }
        else
        {
            AreaPolygon.Points = null;
        }
    }
}
