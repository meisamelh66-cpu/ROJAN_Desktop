namespace Rojan.Desktop.Application.Help;

/// <summary>Phase 26: Help Search. One ranked search hit - <see cref="Snippet"/> is a short excerpt (from whichever field matched) for the results list, with <see cref="TitleHighlights"/>/<see cref="SnippetHighlights"/> marking the matched substrings within <see cref="Title"/>/<see cref="Snippet"/> respectively.</summary>
public sealed record HelpSearchResultDto(
    string TopicId,
    string Title,
    string Snippet,
    double Score,
    IReadOnlyList<HighlightSpan> TitleHighlights,
    IReadOnlyList<HighlightSpan> SnippetHighlights);
