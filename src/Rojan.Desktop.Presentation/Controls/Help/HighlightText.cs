using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Presentation.Controls.Help;

/// <summary>
/// Phase 26.6: "highlight matches" for Help Search results. <see cref="TextBlock.Inlines"/>
/// isn't directly data-bindable, so this attached-property pair rebuilds
/// a TextBlock's <see cref="Run"/>s whenever the <c>Text</c> or
/// <c>Highlights</c> attached property changes, bolding the matched spans
/// <c>HelpSearchService</c> already computed rather than re-deriving them
/// in the View.
/// </summary>
public static class HighlightText
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(HighlightText), new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty HighlightsProperty =
        DependencyProperty.RegisterAttached("Highlights", typeof(IReadOnlyList<HighlightSpan>), typeof(HighlightText), new PropertyMetadata(null, OnChanged));

    public static string? GetText(DependencyObject element) => (string?)element.GetValue(TextProperty);

    public static void SetText(DependencyObject element, string? value) => element.SetValue(TextProperty, value);

    public static IReadOnlyList<HighlightSpan>? GetHighlights(DependencyObject element) => (IReadOnlyList<HighlightSpan>?)element.GetValue(HighlightsProperty);

    public static void SetHighlights(DependencyObject element, IReadOnlyList<HighlightSpan>? value) => element.SetValue(HighlightsProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            Rebuild(textBlock);
        }
    }

    private static void Rebuild(TextBlock textBlock)
    {
        var text = GetText(textBlock) ?? string.Empty;
        var highlights = GetHighlights(textBlock);

        textBlock.Inlines.Clear();
        if (text.Length == 0)
        {
            return;
        }

        if (highlights is null || highlights.Count == 0)
        {
            textBlock.Inlines.Add(new Run(text));
            return;
        }

        var highlightBrush = textBlock.TryFindResource("Rojan.Brush.AccentSubtle") as Brush;
        var position = 0;
        foreach (var span in highlights.OrderBy(h => h.Start))
        {
            var start = Math.Clamp(span.Start, 0, text.Length);
            if (start > position)
            {
                textBlock.Inlines.Add(new Run(text[position..start]));
            }

            var length = Math.Clamp(span.Length, 0, text.Length - start);
            if (length > 0)
            {
                textBlock.Inlines.Add(new Run(text.Substring(start, length))
                {
                    FontWeight = FontWeights.Bold,
                    Background = highlightBrush,
                });
            }

            position = start + length;
        }

        if (position < text.Length)
        {
            textBlock.Inlines.Add(new Run(text[position..]));
        }
    }
}
