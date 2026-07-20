using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Application.Tests.Notifications;

/// <summary>Exercises <see cref="NotificationSearchService"/>'s weighted scoring, case-insensitive matching, and highlight-span computation.</summary>
public sealed class NotificationSearchServiceTests
{
    private readonly NotificationSearchService _service = new();

    [Fact]
    public void Search_EmptyQuery_ReturnsEveryCandidateUnranked()
    {
        var candidates = new[] { new NotificationSearchCandidate("n1", "Low stock warning", "Stock is low") };

        var results = _service.Search(candidates, "   ");

        var result = Assert.Single(results);
        Assert.Equal(0, result.Score);
        Assert.Empty(result.TitleHighlights);
    }

    [Fact]
    public void Search_NoMatchingCandidates_ReturnsNoResults()
    {
        var candidates = new[] { new NotificationSearchCandidate("n1", "Low stock warning", "Stock is low") };

        var results = _service.Search(candidates, "booking");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        var candidates = new[] { new NotificationSearchCandidate("n1", "Low stock warning", "Stock is low") };

        var results = _service.Search(candidates, "STOCK");

        Assert.Single(results);
    }

    [Fact]
    public void Search_TitleMatch_ScoresHigherThanMessageOnlyMatch()
    {
        var candidates = new[]
        {
            new NotificationSearchCandidate("message-only", "Backup completed", "mentions sync once"),
            new NotificationSearchCandidate("title-match", "Sync failed", "connectivity issue"),
        };

        var results = _service.Search(candidates, "sync");

        Assert.Equal(2, results.Count);
        Assert.Equal("title-match", results[0].NotificationId);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Search_TitleHighlight_MarksExactMatchedSpan()
    {
        var candidates = new[] { new NotificationSearchCandidate("n1", "Booking confirmed", "description") };

        var results = _service.Search(candidates, "Booking");

        var highlight = Assert.Single(results[0].TitleHighlights);
        Assert.Equal(0, highlight.Start);
        Assert.Equal("Booking".Length, highlight.Length);
    }

    [Fact]
    public void Search_MultipleOccurrencesInSameField_AllAreHighlighted()
    {
        var candidates = new[] { new NotificationSearchCandidate("n1", "cat cat cat", "description") };

        var results = _service.Search(candidates, "cat");

        Assert.Equal(3, results[0].TitleHighlights.Count);
    }

    [Fact]
    public void Search_ResultsOrderedByScoreDescending()
    {
        var candidates = new[]
        {
            new NotificationSearchCandidate("low", "Something Else", "customer appears once"),
            new NotificationSearchCandidate("high", "Customers", "customer customer customer"),
        };

        var results = _service.Search(candidates, "customer");

        Assert.Equal("high", results[0].NotificationId);
        Assert.Equal("low", results[1].NotificationId);
    }
}
