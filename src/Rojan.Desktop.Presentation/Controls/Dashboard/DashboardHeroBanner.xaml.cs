using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Dashboard hero banner - see the XAML's own comment. The image is decoded here (not through a
/// converter) so an undecodable admin upload degrades to the fallback surface instead of throwing
/// from inside the binding engine; the CTA opens only an absolute http(s) <see cref="CtaUri"/>
/// (already validated by the ViewModel) and is hidden without one.
/// </summary>
public partial class DashboardHeroBanner : UserControl
{
    /// <summary>Banner height is width x this ratio (16:4), clamped to [<see cref="MinBannerHeight"/>, <see cref="MaxBannerHeight"/>].</summary>
    public const double HeightToWidthRatio = 0.25;

    public const double MinBannerHeight = 160;

    public const double MaxBannerHeight = 300;

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(DashboardHeroBanner), new PropertyMetadata(null, OnTextChanged));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(DashboardHeroBanner), new PropertyMetadata(null, OnTextChanged));

    public static readonly DependencyProperty ImageBytesProperty =
        DependencyProperty.Register(nameof(ImageBytes), typeof(byte[]), typeof(DashboardHeroBanner), new PropertyMetadata(null, OnImageBytesChanged));

    public static readonly DependencyProperty CtaLabelProperty =
        DependencyProperty.Register(nameof(CtaLabel), typeof(string), typeof(DashboardHeroBanner), new PropertyMetadata(null));

    public static readonly DependencyProperty CtaUriProperty =
        DependencyProperty.Register(nameof(CtaUri), typeof(Uri), typeof(DashboardHeroBanner), new PropertyMetadata(null, OnCtaUriChanged));

    public DashboardHeroBanner()
    {
        InitializeComponent();
        UpdateImage();
        UpdateText();
        UpdateCta();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public byte[]? ImageBytes
    {
        get => (byte[]?)GetValue(ImageBytesProperty);
        set => SetValue(ImageBytesProperty, value);
    }

    public string? CtaLabel
    {
        get => (string?)GetValue(CtaLabelProperty);
        set => SetValue(CtaLabelProperty, value);
    }

    public Uri? CtaUri
    {
        get => (Uri?)GetValue(CtaUriProperty);
        set => SetValue(CtaUriProperty, value);
    }

    /// <summary>The responsive height rule - public and static so it is testable without constructing a WPF control.</summary>
    public static double ComputeHeight(double width) =>
        Math.Clamp(width * HeightToWidthRatio, MinBannerHeight, MaxBannerHeight);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DashboardHeroBanner)d).UpdateText();

    private static void OnImageBytesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DashboardHeroBanner)d).UpdateImage();

    private static void OnCtaUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DashboardHeroBanner)d).UpdateCta();

    private static BitmapImage? TryDecode(byte[]? bytes)
    {
        if (bytes is not { Length: > 0 })
        {
            return null;
        }

#pragma warning disable CA1031 // Any decode failure (corrupt/unsupported upload) just means "show the fallback surface".
        try
        {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
#pragma warning restore CA1031
    }

    private void UpdateImage()
    {
        if (BannerImage is null)
        {
            return;
        }

        var image = TryDecode(ImageBytes);
        BannerImage.Source = image;
        BannerImage.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateText()
    {
        if (TitleText is null)
        {
            return;
        }

        TitleText.Visibility = string.IsNullOrWhiteSpace(Title) ? Visibility.Collapsed : Visibility.Visible;
        SubtitleText.Visibility = string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
        UpdateScrim();
    }

    private void UpdateCta()
    {
        if (CtaButton is null)
        {
            return;
        }

        CtaButton.Visibility = CtaUri is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateScrim();
    }

    // An image-only banner (no title, subtitle or CTA) shows the artwork unshaded.
    private void UpdateScrim()
    {
        if (TextScrim is null || TitleText is null || CtaButton is null)
        {
            return;
        }

        var hasOverlayContent = TitleText.Visibility == Visibility.Visible
            || SubtitleText.Visibility == Visibility.Visible
            || CtaButton.Visibility == Visibility.Visible;
        TextScrim.Visibility = hasOverlayContent ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Card_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            var height = ComputeHeight(e.NewSize.Width);
            if (Math.Abs(Card.Height - height) > 0.5 || double.IsNaN(Card.Height))
            {
                Card.Height = height;
            }
        }

        // Clip the image/scrim to the card's rounded corners (a plain Border does not clip its child).
        var radius = Card.CornerRadius.TopLeft;
        ClipHost.Clip = new RectangleGeometry(new Rect(0, 0, Card.ActualWidth, Card.ActualHeight), radius, radius);
    }

    private void CtaButton_Click(object sender, RoutedEventArgs e)
    {
        if (CtaUri is not { } uri)
        {
            return;
        }

#pragma warning disable CA1031 // No default browser / blocked shell: the Dashboard must keep working.
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Dashboard banner CTA could not open '{uri}': {ex.Message}");
        }
#pragma warning restore CA1031
    }
}
