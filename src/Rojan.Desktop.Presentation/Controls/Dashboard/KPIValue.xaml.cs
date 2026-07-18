using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Label + value pair for a single KPI reading. No logic beyond the two dependency properties.</summary>
public partial class KPIValue : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(KPIValue),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(KPIValue),
            new PropertyMetadata(string.Empty));

    public KPIValue()
    {
        InitializeComponent();
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
}
