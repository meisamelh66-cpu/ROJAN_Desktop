using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// Icon + label + value + optional trend pill - the shared KPI tile every
/// module's hero strip should use instead of a bare number (see the
/// Sprint 1 Premium Kit plan). Deliberately self-contained rather than
/// composing Controls.Dashboard.KPIValue/TrendIndicator: those move into
/// this same Controls.Shared namespace in a later commit, and this control
/// needs to exist and build cleanly before that move happens. Once
/// KPIValue/TrendIndicator land here, StatTile can be refactored to host
/// them internally without changing its own public DP surface.
/// </summary>
public partial class StatTile : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TrendDirectionProperty =
        DependencyProperty.Register(nameof(TrendDirection), typeof(StatTileTrend), typeof(StatTile), new PropertyMetadata(StatTileTrend.None));

    public static readonly DependencyProperty TrendTextProperty =
        DependencyProperty.Register(nameof(TrendText), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public StatTile()
    {
        InitializeComponent();
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public StatTileTrend TrendDirection
    {
        get => (StatTileTrend)GetValue(TrendDirectionProperty);
        set => SetValue(TrendDirectionProperty, value);
    }

    public string TrendText
    {
        get => (string)GetValue(TrendTextProperty);
        set => SetValue(TrendTextProperty, value);
    }
}
