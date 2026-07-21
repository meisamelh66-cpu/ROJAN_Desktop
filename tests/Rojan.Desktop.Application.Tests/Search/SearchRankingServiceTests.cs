using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Application.Tests.Search;

/// <summary>Exercises <see cref="SearchRankingService"/>'s candidate matching, type-priority/favorite boosts, highlight-span mapping, and empty-query behavior.</summary>
public sealed class SearchRankingServiceTests
{
    private readonly SearchRankingService _service = new();
    private static readonly IReadOnlySet<string> NoFavorites = new HashSet<string>();

    private static SearchCandidate Candidate(string id, SearchResultType type, string title, IReadOnlyList<string>? keywords = null) =>
        new(id, type, title, string.Empty, keywords ?? [], $"page:{id}");

    [Fact]
    public void Rank_EmptyQuery_ReturnsNoResults()
    {
        var candidates = new[] { Candidate("c1", SearchResultType.Customer, "John Doe") };

        var results = _service.Rank(candidates, "   ", NoFavorites);

        Assert.Empty(results);
    }

    [Fact]
    public void Rank_NoMatchingCandidates_ReturnsNoResults()
    {
        var candidates = new[] { Candidate("c1", SearchResultType.Customer, "John Doe") };

        var results = _service.Rank(candidates, "xyz123", NoFavorites);

        Assert.Empty(results);
    }

    [Fact]
    public void Rank_TitleMatch_IsReturnedWithHighlights()
    {
        var candidates = new[] { Candidate("c1", SearchResultType.Customer, "John Doe") };

        var results = _service.Rank(candidates, "John", NoFavorites);

        var result = Assert.Single(results);
        Assert.NotEmpty(result.TitleHighlights);
    }

    [Fact]
    public void Rank_KeywordOnlyMatch_IsReturnedWithNoHighlights()
    {
        var candidates = new[] { Candidate("c1", SearchResultType.Customer, "John Doe", ["john@acme.com"]) };

        var results = _service.Rank(candidates, "acme", NoFavorites);

        var result = Assert.Single(results);
        Assert.Empty(result.TitleHighlights);
    }

    [Fact]
    public void Rank_EquallyStrongMatch_CommandOutranksBusinessData()
    {
        var candidates = new[]
        {
            Candidate("cust1", SearchResultType.Customer, "Settings"),
            Candidate("cmd1", SearchResultType.Command, "Settings"),
        };

        var results = _service.Rank(candidates, "Settings", NoFavorites);

        Assert.Equal("cmd1", results[0].Id);
    }

    [Fact]
    public void Rank_EquallyStrongMatch_PageOutranksBusinessData()
    {
        var candidates = new[]
        {
            Candidate("cust1", SearchResultType.Customer, "Bookings"),
            Candidate("page1", SearchResultType.Page, "Bookings"),
        };

        var results = _service.Rank(candidates, "Bookings", NoFavorites);

        Assert.Equal("page1", results[0].Id);
    }

    [Fact]
    public void Rank_FavoriteCandidate_ScoresHigherThanEquallyStrongNonFavorite()
    {
        var candidates = new[]
        {
            Candidate("c1", SearchResultType.Customer, "John Doe"),
            Candidate("c2", SearchResultType.Customer, "John Smith"),
        };
        var favorites = new HashSet<string> { "c2" };

        var results = _service.Rank(candidates, "John", favorites);

        Assert.Equal("c2", results[0].Id);
        Assert.True(results[0].IsFavorite);
        Assert.False(results[1].IsFavorite);
    }

    [Fact]
    public void Rank_StrongerTextMatchOutranksTypePriorityAlone()
    {
        // An exact title match on a Customer should still beat a merely
        // fuzzy match on a Command - type priority is a small bonus, not
        // enough to override a real difference in match quality.
        var candidates = new[]
        {
            Candidate("cust1", SearchResultType.Customer, "Sarah Johnson"),
            Candidate("cmd1", SearchResultType.Command, "Toggle Sidebar"),
        };

        var results = _service.Rank(candidates, "Sarah Johnson", NoFavorites);

        Assert.Single(results);
        Assert.Equal("cust1", results[0].Id);
    }

    [Fact]
    public void Rank_ResultsOrderedByScoreDescending()
    {
        var candidates = new[]
        {
            Candidate("exact", SearchResultType.Customer, "Test"),
            Candidate("prefix", SearchResultType.Customer, "Testing"),
            Candidate("substring", SearchResultType.Customer, "Latest"),
        };

        var results = _service.Rank(candidates, "Test", NoFavorites);

        Assert.Equal(3, results.Count);
        Assert.Equal("exact", results[0].Id);
        Assert.Equal("prefix", results[1].Id);
        Assert.Equal("substring", results[2].Id);
    }
}
