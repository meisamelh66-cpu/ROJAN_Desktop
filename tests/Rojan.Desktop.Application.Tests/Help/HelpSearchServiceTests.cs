using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Application.Tests.Help;

/// <summary>Exercises <see cref="HelpSearchService"/>'s weighted scoring, case-insensitive matching, and highlight-span computation.</summary>
public sealed class HelpSearchServiceTests
{
    private readonly HelpSearchService _service = new();

    [Fact]
    public void Search_EmptyQuery_ReturnsNoResults()
    {
        var candidates = new[] { new HelpSearchCandidate("t1", "Customers", "Manage your customers", "Overview") };

        var results = _service.Search(candidates, "   ");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_NoMatchingCandidates_ReturnsNoResults()
    {
        var candidates = new[] { new HelpSearchCandidate("t1", "Customers", "Manage your customers", "Overview") };

        var results = _service.Search(candidates, "inventory");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        var candidates = new[] { new HelpSearchCandidate("t1", "Customers", "Manage your customers", "Overview") };

        var results = _service.Search(candidates, "CUSTOMERS");

        Assert.Single(results);
    }

    [Fact]
    public void Search_TitleMatch_ScoresHigherThanOverviewOnlyMatch()
    {
        var candidates = new[]
        {
            new HelpSearchCandidate("overview-only", "Dashboard", "General summary", "mentions bookings once"),
            new HelpSearchCandidate("title-match", "Bookings", "Schedule appointments", "Overview text"),
        };

        var results = _service.Search(candidates, "bookings");

        Assert.Equal(2, results.Count);
        Assert.Equal("title-match", results[0].TopicId);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Search_ResultsAreOrderedByScoreDescending()
    {
        var candidates = new[]
        {
            new HelpSearchCandidate("low", "Something Else", "no match here", "customer appears once"),
            new HelpSearchCandidate("high", "Customers", "customer customer customer", "customer"),
        };

        var results = _service.Search(candidates, "customer");

        Assert.Equal("high", results[0].TopicId);
        Assert.Equal("low", results[1].TopicId);
    }

    [Fact]
    public void Search_TitleHighlight_MarksExactMatchedSpan()
    {
        var candidates = new[] { new HelpSearchCandidate("t1", "Customer Management", "description", "overview") };

        var results = _service.Search(candidates, "Customer");

        var highlight = Assert.Single(results[0].TitleHighlights);
        Assert.Equal(0, highlight.Start);
        Assert.Equal("Customer".Length, highlight.Length);
    }

    [Fact]
    public void Search_MultipleOccurrencesInSameField_AllAreHighlighted()
    {
        var candidates = new[] { new HelpSearchCandidate("t1", "cat cat cat", "description", "overview") };

        var results = _service.Search(candidates, "cat");

        Assert.Equal(3, results[0].TitleHighlights.Count);
    }

    [Fact]
    public void Search_ReturnsOneResultPerMatchingCandidate()
    {
        var candidates = new[]
        {
            new HelpSearchCandidate("t1", "Customers", "d", "o"),
            new HelpSearchCandidate("t2", "Bookings", "d", "o"),
        };

        var results = _service.Search(candidates, "d");

        Assert.Equal(2, results.Count);
    }
}
