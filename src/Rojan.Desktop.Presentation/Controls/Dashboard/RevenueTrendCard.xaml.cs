using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Phase C-1 (Analytics Row): reusable, decoupled from any specific
/// ViewModel via its own Dependency Properties - same shape as
/// AiHeroBanner/NewsTicker's ItemsSource, not an ambient DataContext
/// binding.
/// </summary>
public partial class RevenueTrendCard : UserControl
{
    // Mirrors SimpleBarChart.RebuildLine's own constants exactly (Controls/
    // Analytics/SimpleBarChart.xaml.cs) - not modifying that file, but using
    // the identical coordinate formula here guarantees each label lands
    // under its real data point regardless of how FlowDirection ends up
    // affecting either Canvas, since both are unmirrored siblings using the
    // same math.
    private const double ChartWidth = 320;
    private const double ChartPadding = 8;

    public static readonly DependencyProperty ChartProperty =
        DependencyProperty.Register(nameof(Chart), typeof(ChartDefinitionDto), typeof(RevenueTrendCard), new PropertyMetadata(null));

    public static readonly DependencyProperty DayLabelsProperty =
        DependencyProperty.Register(nameof(DayLabels), typeof(IReadOnlyList<string>), typeof(RevenueTrendCard), new PropertyMetadata(null, OnDayLabelsChanged));

    public RevenueTrendCard()
    {
        InitializeComponent();
        Loaded += (_, _) => PositionDayLabels();
    }

    public ChartDefinitionDto? Chart
    {
        get => (ChartDefinitionDto?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    public IReadOnlyList<string>? DayLabels
    {
        get => (IReadOnlyList<string>?)GetValue(DayLabelsProperty);
        set => SetValue(DayLabelsProperty, value);
    }

    private static void OnDayLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RevenueTrendCard card)
        {
            card.PositionDayLabels();
        }
    }

    private void PositionDayLabels()
    {
        DayLabelsCanvas.Children.Clear();

        var labels = DayLabels;
        if (labels is null || labels.Count == 0)
        {
            return;
        }

        var stepX = labels.Count <= 1 ? 0d : (ChartWidth - (2 * ChartPadding)) / (labels.Count - 1);
        var typeface = new Typeface(
            (FontFamily)FindResource("Rojan.FontFamily.Default"),
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);
        var foreground = (Brush)FindResource("Rojan.Brush.MutedText");

        for (var i = 0; i < labels.Count; i++)
        {
            var formattedText = new FormattedText(
                labels[i],
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var textBlock = new TextBlock
            {
                Text = labels[i],
                FontFamily = typeface.FontFamily,
                FontSize = 12,
                Foreground = foreground,
            };

            var x = ChartPadding + (i * stepX);
            Canvas.SetLeft(textBlock, x - (formattedText.Width / 2));
            Canvas.SetTop(textBlock, 0);
            DayLabelsCanvas.Children.Add(textBlock);
        }
    }
}
