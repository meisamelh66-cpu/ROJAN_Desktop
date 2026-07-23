using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>Phase C-1 (Analytics Row): reusable, decoupled from any specific ViewModel via its own Dependency Properties.</summary>
public partial class SalonHealthCard : UserControl
{
    public static readonly DependencyProperty ChartProperty =
        DependencyProperty.Register(nameof(Chart), typeof(ChartDefinitionDto), typeof(SalonHealthCard), new PropertyMetadata(null));

    public static readonly DependencyProperty ScoreTextProperty =
        DependencyProperty.Register(nameof(ScoreText), typeof(string), typeof(SalonHealthCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusLabelProperty =
        DependencyProperty.Register(nameof(StatusLabel), typeof(string), typeof(SalonHealthCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SalonHealthCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailsCommandProperty =
        DependencyProperty.Register(nameof(DetailsCommand), typeof(ICommand), typeof(SalonHealthCard), new PropertyMetadata(null));

    public SalonHealthCard()
    {
        InitializeComponent();
    }

    public ChartDefinitionDto? Chart
    {
        get => (ChartDefinitionDto?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    public string ScoreText
    {
        get => (string)GetValue(ScoreTextProperty);
        set => SetValue(ScoreTextProperty, value);
    }

    public string StatusLabel
    {
        get => (string)GetValue(StatusLabelProperty);
        set => SetValue(StatusLabelProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public ICommand? DetailsCommand
    {
        get => (ICommand?)GetValue(DetailsCommandProperty);
        set => SetValue(DetailsCommandProperty, value);
    }
}
