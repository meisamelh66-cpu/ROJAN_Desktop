using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>Colored severity pill - see StatusPill.xaml's own doc comment for the visual technique it reuses from TrendIndicator.</summary>
public partial class StatusPill : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusPill), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(PillSeverity), typeof(StatusPill), new PropertyMetadata(PillSeverity.Neutral));

    public StatusPill()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public PillSeverity Severity
    {
        get => (PillSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }
}
