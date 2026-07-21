namespace Rojan.Desktop.Application.Search;

/// <summary>Phase 28: Enterprise Global Search &amp; Command Palette. Ranks a candidate set against a query - the Intelligent Ranking, Fuzzy Matching, and Search Highlighting requirements, all in one pass.</summary>
public interface ISearchRankingService
{
    /// <summary>
    /// Ranked matches for <paramref name="query"/> among
    /// <paramref name="candidates"/>, highest score first. An empty or
    /// whitespace-only query returns an empty list - unlike Notification
    /// Search's "browse everything" default, the palette's candidate set
    /// can include hundreds of customers/bookings/products, so an
    /// unranked full dump would not be useful; the empty-query state is
    /// Recent Searches/Favorites instead (a Presentation concern, not
    /// this service's).
    /// </summary>
    public IReadOnlyList<SearchResultDto> Rank(IReadOnlyList<SearchCandidate> candidates, string query, IReadOnlySet<string> favoriteIds);
}
