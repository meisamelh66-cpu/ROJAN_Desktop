using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Rojan.Desktop.Application.Dashboard;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Phase 34 (Premium KPI Chart Visualization): a per-KPI mini chart, built
/// only from the two real values already bound to this control (current
/// Value + a previous-period value derived from TrendDirection/
/// TrendPercentage - see KpiNumberParsing). Chart kind and accent color are
/// resolved from the KPI's stable Id, mirroring KpiIconConverter's existing
/// id switch, with the same safe fallback (an unrecognized id still renders
/// a sensible default line chart rather than throwing or showing nothing).
/// </summary>
public partial class KpiChart : UserControl
{
    private static readonly TimeSpan LineRevealDuration = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan BarGrowDuration = TimeSpan.FromMilliseconds(650);

    public static readonly DependencyProperty IdProperty =
        DependencyProperty.Register(
            nameof(Id),
            typeof(string),
            typeof(KpiChart),
            new PropertyMetadata(string.Empty, OnDataChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(KpiChart),
            new PropertyMetadata(string.Empty, OnDataChanged));

    public static readonly DependencyProperty TrendDirectionProperty =
        DependencyProperty.Register(
            nameof(TrendDirection),
            typeof(TrendDirection),
            typeof(KpiChart),
            new PropertyMetadata(TrendDirection.Flat, OnDataChanged));

    public static readonly DependencyProperty TrendPercentageProperty =
        DependencyProperty.Register(
            nameof(TrendPercentage),
            typeof(double),
            typeof(KpiChart),
            new PropertyMetadata(0.0, OnDataChanged));

    public KpiChart()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public TrendDirection TrendDirection
    {
        get => (TrendDirection)GetValue(TrendDirectionProperty);
        set => SetValue(TrendDirectionProperty, value);
    }

    public double TrendPercentage
    {
        get => (double)GetValue(TrendPercentageProperty);
        set => SetValue(TrendPercentageProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((KpiChart)d).Redraw();

    private void LineCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (!KpiNumberParsing.TryParse(Value, out var current, out _, out _, out _, out _))
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        Visibility = Visibility.Visible;
        var previous = KpiNumberParsing.ComputePrevious(current, TrendDirection, TrendPercentage);
        var config = ResolveConfig(Id);

        if (config.Kind == ChartKind.Bar)
        {
            LineCanvas.Visibility = Visibility.Collapsed;
            BarGrid.Visibility = Visibility.Visible;
            RedrawBar(previous, current, config.Accent);
        }
        else
        {
            BarGrid.Visibility = Visibility.Collapsed;
            LineCanvas.Visibility = Visibility.Visible;
            RedrawLine(previous, current, config.Accent, config.Glow);
        }
    }

    private void RedrawLine(double previous, double current, Color accent, Color glow)
    {
        var width = LineCanvas.ActualWidth;
        var height = LineCanvas.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        const double topPad = 7;
        const double bottomPad = 7;
        var min = Math.Min(previous, current);
        var max = Math.Max(previous, current);
        var usableHeight = Math.Max(1.0, height - topPad - bottomPad);

        double YFor(double v)
        {
            if (max - min < 1e-9)
            {
                return height / 2;
            }

            var t = (v - min) / (max - min);
            return height - bottomPad - (t * usableHeight);
        }

        var start = new Point(0, YFor(previous));
        var end = new Point(width, YFor(current));
        var control1 = new Point(width * 0.42, start.Y);
        var control2 = new Point(width * 0.58, end.Y);

        var lineFigure = new PathFigure { StartPoint = start, IsClosed = false };
        lineFigure.Segments.Add(new BezierSegment(control1, control2, end, true));
        var lineGeometry = new PathGeometry();
        lineGeometry.Figures.Add(lineFigure);

        var areaFigure = new PathFigure { StartPoint = start, IsClosed = true };
        areaFigure.Segments.Add(new BezierSegment(control1, control2, end, true));
        areaFigure.Segments.Add(new LineSegment(new Point(width, height), true));
        areaFigure.Segments.Add(new LineSegment(new Point(0, height), true));
        var areaGeometry = new PathGeometry();
        areaGeometry.Figures.Add(areaFigure);

        LinePath.Data = lineGeometry;
        LinePath.Stroke = new SolidColorBrush(accent);

        GlowPath.Data = lineGeometry;
        GlowPath.Stroke = new SolidColorBrush(glow);

        AreaPath.Data = areaGeometry;
        AreaPath.Fill = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(70, accent.R, accent.G, accent.B), 0),
                new GradientStop(Color.FromArgb(0, accent.R, accent.G, accent.B), 1),
            },
        };

        StartDot.Fill = new SolidColorBrush(accent);
        Canvas.SetLeft(StartDot, start.X - (StartDot.Width / 2));
        Canvas.SetTop(StartDot, start.Y - (StartDot.Height / 2));

        EndDot.Fill = new SolidColorBrush(accent);
        Canvas.SetLeft(EndDot, end.X - (EndDot.Width / 2));
        Canvas.SetTop(EndDot, end.Y - (EndDot.Height / 2));

        var clipGeometry = new RectangleGeometry(new Rect(0, -topPad, 0, height + topPad + bottomPad));
        LineCanvas.Clip = clipGeometry;
        var reveal = new RectAnimation
        {
            From = new Rect(0, -topPad, 0, height + topPad + bottomPad),
            To = new Rect(0, -topPad, width, height + topPad + bottomPad),
            Duration = LineRevealDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        clipGeometry.BeginAnimation(RectangleGeometry.RectProperty, reveal);

        var fadeIn = new DoubleAnimation(0, 1, LineRevealDuration) { BeginTime = TimeSpan.FromMilliseconds(150) };
        GlowPath.BeginAnimation(OpacityProperty, fadeIn);
        AreaPath.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void RedrawBar(double previous, double current, Color accent)
    {
        const double maxBarHeight = 38;
        const double minBarHeight = 4;
        var max = Math.Max(Math.Max(previous, current), 0.0001);

        var previousHeight = Math.Max(minBarHeight, (previous / max) * maxBarHeight);
        var currentHeight = Math.Max(minBarHeight, (current / max) * maxBarHeight);

        PreviousBar.Height = previousHeight;
        PreviousBar.CornerRadius = new CornerRadius(6, 6, 0, 0);
        PreviousBar.Background = new SolidColorBrush(Color.FromArgb(70, accent.R, accent.G, accent.B));

        CurrentBar.Height = currentHeight;
        CurrentBar.CornerRadius = new CornerRadius(6, 6, 0, 0);
        CurrentBar.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop(accent, 0),
                new GradientStop(Color.FromArgb(170, accent.R, accent.G, accent.B), 1),
            },
        };

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        PreviousBarScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0, 1, BarGrowDuration) { EasingFunction = easing });
        CurrentBarScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0, 1, BarGrowDuration)
        {
            BeginTime = TimeSpan.FromMilliseconds(120),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });
    }

    private static ChartConfig ResolveConfig(string? id) => id switch
    {
        "kpi-revenue" => new ChartConfig(ChartKind.Line, ResolveColor("Rojan.Color.Accent")),
        "kpi-clients" => new ChartConfig(ChartKind.Line, ResolveColor("Rojan.Color.Success")),
        "kpi-bookings" => new ChartConfig(ChartKind.Bar, ResolveColor("Rojan.Color.AccentLavender")),
        "kpi-tasks" => new ChartConfig(ChartKind.Line, ResolveColor("Rojan.Color.Warning")),
        _ => new ChartConfig(ChartKind.Line, ResolveColor("Rojan.Color.Accent")),
    };

    private static Color ResolveColor(string key) =>
        System.Windows.Application.Current.TryFindResource(key) is Color color ? color : Colors.Gray;

    private enum ChartKind
    {
        Line,
        Bar,
    }

    private readonly record struct ChartConfig(ChartKind Kind, Color Accent)
    {
        public Color Glow => Accent;
    }
}
