using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Phase B-1: AI Hero Banner. Every piece of content is a Dependency
/// Property (not an ambient DataContext binding) so this control stays
/// reusable independent of DashboardPageViewModel - the same decoupling
/// NewsTicker's ItemsSource DP already establishes for this codebase's
/// Dashboard controls.
/// </summary>
public partial class AiHeroBanner : UserControl
{
    public static readonly DependencyProperty TagLabelProperty =
        DependencyProperty.Register(nameof(TagLabel), typeof(string), typeof(AiHeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeadlineProperty =
        DependencyProperty.Register(nameof(Headline), typeof(string), typeof(AiHeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(AiHeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CtaLabelProperty =
        DependencyProperty.Register(nameof(CtaLabel), typeof(string), typeof(AiHeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ViewSuggestionsCommandProperty =
        DependencyProperty.Register(nameof(ViewSuggestionsCommand), typeof(ICommand), typeof(AiHeroBanner), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryCommandProperty =
        DependencyProperty.Register(nameof(SecondaryCommand), typeof(ICommand), typeof(AiHeroBanner), new PropertyMetadata(null));

    public AiHeroBanner()
    {
        InitializeComponent();
    }

    public string TagLabel
    {
        get => (string)GetValue(TagLabelProperty);
        set => SetValue(TagLabelProperty, value);
    }

    public string Headline
    {
        get => (string)GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string CtaLabel
    {
        get => (string)GetValue(CtaLabelProperty);
        set => SetValue(CtaLabelProperty, value);
    }

    public ICommand? ViewSuggestionsCommand
    {
        get => (ICommand?)GetValue(ViewSuggestionsCommandProperty);
        set => SetValue(ViewSuggestionsCommandProperty, value);
    }

    public ICommand? SecondaryCommand
    {
        get => (ICommand?)GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }
}
