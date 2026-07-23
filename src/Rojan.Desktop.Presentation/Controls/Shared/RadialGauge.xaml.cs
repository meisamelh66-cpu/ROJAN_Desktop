using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// Phase C-2: draws <see cref="Percentage"/> as a clockwise arc starting at
/// 12 o'clock - the same angle convention (angle-90 offset) SimpleBarChart's
/// own PointOnCircle helper uses for its pie wedges, reapplied here as a
/// small independent formula (not a shared/reused method - this control has
/// no dependency on Controls/Analytics), since a single-arc primitive is
/// too small a thing to justify a cross-namespace reference.
/// </summary>
public partial class RadialGauge : UserControl
{
    private const double Radius = 19;
    private const double Center = 22;

    public static readonly DependencyProperty PercentageProperty =
        DependencyProperty.Register(nameof(Percentage), typeof(int), typeof(RadialGauge), new PropertyMetadata(0, OnPercentageChanged));

    public static readonly DependencyProperty RingBrushProperty =
        DependencyProperty.Register(nameof(RingBrush), typeof(Brush), typeof(RadialGauge), new PropertyMetadata(null, OnPercentageChanged));

    public RadialGauge()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    public int Percentage
    {
        get => (int)GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public Brush? RingBrush
    {
        get => (Brush?)GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    private static void OnPercentageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RadialGauge ring)
        {
            ring.Redraw();
        }
    }

    private void Redraw()
    {
        var brush = RingBrush ?? (Brush)FindResource("Rojan.Brush.Accent");
        ProgressArc.Stroke = brush;

        var clamped = Math.Clamp(Percentage, 0, 100);
        PercentageTextBlock.Text = clamped.ToString(CultureInfo.CurrentCulture);

        if (clamped <= 0)
        {
            ProgressArc.Data = null;
            return;
        }

        if (clamped >= 100)
        {
            ProgressArc.Data = new EllipseGeometry(new Point(Center, Center), Radius, Radius);
            return;
        }

        var sweepAngle = clamped / 100d * 360d;
        var start = PointOnCircle(0);
        var end = PointOnCircle(sweepAngle);
        var isLargeArc = sweepAngle > 180d;

        var figure = new PathFigure { StartPoint = start, IsClosed = false };
        figure.Segments.Add(new ArcSegment(end, new Size(Radius, Radius), 0, isLargeArc, SweepDirection.Clockwise, true));
        ProgressArc.Data = new PathGeometry([figure]);
    }

    private static Point PointOnCircle(double angleDegrees)
    {
        var radians = (Math.PI / 180d) * (angleDegrees - 90d);
        return new Point(Center + (Radius * Math.Cos(radians)), Center + (Radius * Math.Sin(radians)));
    }
}
