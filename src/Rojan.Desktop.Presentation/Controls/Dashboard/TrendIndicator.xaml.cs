using System.Windows;
using System.Windows.Controls;
using Rojan.Desktop.Application.Dashboard;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Up/down/flat glyph + percentage, colored via the semantic success/error/muted brushes. No logic beyond the two dependency properties - direction-to-visual mapping is declarative (DataTriggers in the XAML), not code-behind.</summary>
public partial class TrendIndicator : UserControl
{
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register(
            nameof(Direction),
            typeof(TrendDirection),
            typeof(TrendIndicator),
            new PropertyMetadata(TrendDirection.Flat));

    public static readonly DependencyProperty PercentageProperty =
        DependencyProperty.Register(
            nameof(Percentage),
            typeof(double),
            typeof(TrendIndicator),
            new PropertyMetadata(0.0));

    public TrendIndicator()
    {
        InitializeComponent();
    }

    public TrendDirection Direction
    {
        get => (TrendDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public double Percentage
    {
        get => (double)GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }
}
