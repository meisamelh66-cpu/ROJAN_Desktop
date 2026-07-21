using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Presentation.Controls.Search;

/// <summary>
/// Phase 28: Global Search's "highlight matches" - the same attached-
/// property-pair shape as <c>Controls.Help.HighlightText</c>/
/// <c>Controls.Notifications.NotificationHighlightText</c>, duplicated
/// rather than shared because it operates on
/// <see cref="Application.Search.HighlightSpan"/>, a distinct type from
/// the other two features' own copies (each vertical slice owns its own
/// DTOs in this codebase, never shared across modules).
/// </summary>
public static class SearchHighlightText
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(SearchHighlightText), new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty HighlightsProperty =
        DependencyProperty.RegisterAttached("Highlights", typeof(IReadOnlyList<HighlightSpan>), typeof(SearchHighlightText), new PropertyMetadata(null, OnChanged));

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
