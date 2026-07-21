namespace Rojan.Desktop.Application.Search;

/// <summary>One ranked <see cref="SearchRankingService"/> match - a <see cref="SearchCandidate"/> plus its score and title highlight spans, sorted highest score first.</summary>
public sealed record SearchResultDto(
    string Id,
    SearchResultType Type,
    string Title,
    string Subtitle,
    string ActionKey,
    double Score,
    bool IsFavorite,
    IReadOnlyList<HighlightSpan> TitleHighlights);
