using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// Generic hero banner: gradient surface, headline/subtitle, one primary
/// CTA and one optional icon-only secondary action, optional artwork.
/// Modeled on Controls/Dashboard/AiHeroBanner.xaml but deliberately not a
/// move - that control's artwork is baked to the specific ROJAN AI mascot
/// image (Phase D-1), not generic. AiHeroBanner can later become a thin
/// consumer of this control without disturbing Dashboard.
/// </summary>
public partial class HeroBanner : UserControl
{
    public static readonly DependencyProperty HeadlineProperty =
        DependencyProperty.Register(nameof(Headline), typeof(string), typeof(HeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(HeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryButtonTextProperty =
        DependencyProperty.Register(nameof(PrimaryButtonText), typeof(string), typeof(HeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryButtonCommandProperty =
        DependencyProperty.Register(nameof(PrimaryButtonCommand), typeof(ICommand), typeof(HeroBanner), new PropertyMetadata(null));

    public static readonly DependencyProperty PrimaryButtonCommandParameterProperty =
        DependencyProperty.Register(nameof(PrimaryButtonCommandParameter), typeof(object), typeof(HeroBanner), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryIconProperty =
        DependencyProperty.Register(nameof(SecondaryIcon), typeof(string), typeof(HeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SecondaryButtonCommandProperty =
        DependencyProperty.Register(nameof(SecondaryButtonCommand), typeof(ICommand), typeof(HeroBanner), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryButtonTooltipProperty =
        DependencyProperty.Register(nameof(SecondaryButtonTooltip), typeof(string), typeof(HeroBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ArtworkProperty =
        DependencyProperty.Register(nameof(Artwork), typeof(ImageSource), typeof(HeroBanner), new PropertyMetadata(null));

    public HeroBanner()
    {
        InitializeComponent();
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

    public string PrimaryButtonText
    {
        get => (string)GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    public ICommand? PrimaryButtonCommand
    {
        get => (ICommand?)GetValue(PrimaryButtonCommandProperty);
        set => SetValue(PrimaryButtonCommandProperty, value);
    }

    public object? PrimaryButtonCommandParameter
    {
        get => GetValue(PrimaryButtonCommandParameterProperty);
        set => SetValue(PrimaryButtonCommandParameterProperty, value);
    }

    public string SecondaryIcon
    {
        get => (string)GetValue(SecondaryIconProperty);
        set => SetValue(SecondaryIconProperty, value);
    }

    public ICommand? SecondaryButtonCommand
    {
        get => (ICommand?)GetValue(SecondaryButtonCommandProperty);
        set => SetValue(SecondaryButtonCommandProperty, value);
    }

    public string SecondaryButtonTooltip
    {
        get => (string)GetValue(SecondaryButtonTooltipProperty);
        set => SetValue(SecondaryButtonTooltipProperty, value);
    }

    public ImageSource? Artwork
    {
        get => (ImageSource?)GetValue(ArtworkProperty);
        set => SetValue(ArtworkProperty, value);
    }
}
