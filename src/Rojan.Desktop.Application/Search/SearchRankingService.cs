using DomainSearch = Rojan.Desktop.Domain.Search;

namespace Rojan.Desktop.Application.Search;

/// <summary>
/// Default <see cref="ISearchRankingService"/>. Matches the query (via
/// <see cref="ISearchRankingService.Rank"/>) against each candidate's
/// <see cref="SearchCandidate.Title"/> (the
/// primary, highlighted match) and <see cref="SearchCandidate.Keywords"/>
/// (a match anywhere boosts recall but is never highlighted, since
/// keywords aren't displayed) using <see cref="DomainSearch.SearchRules.Match"/>
/// for the actual text comparison - keeping the fuzzy-matching algorithm
/// itself in Domain, pure and reusable. Adds a type-priority bonus (
/// Commands and Pages rank above live business data for equally strong
/// text matches - a user typing "cust" most likely wants the Customers
/// page, not a specific customer named "Custine") and a favorite bonus
/// on top of the raw match score.
/// </summary>
public sealed class SearchRankingService : ISearchRankingService
{
    private const double KeywordMatchWeight = 0.6;
    private const double FavoriteBonus = 25;

    public IReadOnlyList<SearchResultDto> Rank(IReadOnlyList<SearchCandidate> candidates, string query, IReadOnlySet<string> favoriteIds)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var results = new List<SearchResultDto>();
        foreach (var candidate in candidates)
        {
            var titleMatch = DomainSearch.SearchRules.Match(query, candidate.Title);
            var bestKeywordScore = 0.0;
            foreach (var keyword in candidate.Keywords)
            {
                var keywordMatch = DomainSearch.SearchRules.Match(query, keyword);
                if (keywordMatch.IsMatch)
                {
                    bestKeywordScore = Math.Max(bestKeywordScore, keywordMatch.Score * KeywordMatchWeight);
                }
            }

            var matchScore = Math.Max(titleMatch.IsMatch ? titleMatch.Score : 0, bestKeywordScore);
            if (matchScore <= 0)
            {
                continue;
            }

            var isFavorite = favoriteIds.Contains(candidate.Id);
            var score = matchScore + TypePriority(candidate.Type) + (isFavorite ? FavoriteBonus : 0);

            results.Add(new SearchResultDto(
                candidate.Id,
                candidate.Type,
                candidate.Title,
                candidate.Subtitle,
                candidate.ActionKey,
                score,
                isFavorite,
                titleMatch.IsMatch ? MapSpans(titleMatch.Spans) : []));
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Commands and Pages surface above live business data for an equally strong text match - see the class doc comment.</summary>
    private static double TypePriority(SearchResultType type) => type switch
    {
        SearchResultType.Command => 15,
        SearchResultType.Page => 10,
        _ => 0,
    };

    private static List<HighlightSpan> MapSpans(IReadOnlyList<DomainSearch.MatchSpan> spans) =>
        spans.Select(s => new HighlightSpan(s.Start, s.Length)).ToList();
}
