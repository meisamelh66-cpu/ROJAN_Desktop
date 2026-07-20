using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

internal sealed class StubContextProvider(string businessContext) : IContextProvider
{
    public Task<string> GetBusinessContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(businessContext);
}

public sealed class SummaryEngineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static AIInsightDto BuildInsight(string id, InsightSeverity severity, decimal changePercent) =>
        new(id, InsightCategory.Revenue, severity, $"Title {id}", $"Description {id}", 100m, changePercent, Now);

    [Fact]
    public async Task GetDailySummaryAsync_UsesBusinessContextAsNarrativeAndHighlightsRiskAndOpportunityInsights()
    {
        IReadOnlyList<AIInsightDto> insights =
        [
            BuildInsight("i-info", InsightSeverity.Info, 1m),
            BuildInsight("i-risk", InsightSeverity.Risk, -15m),
        ];
        var sut = new SummaryEngine(new StubContextProvider("Business snapshot text."), new StubInsightEngine(insights));

        var summary = await sut.GetDailySummaryAsync();

        Assert.Equal("Daily Summary", summary.Title);
        Assert.Equal("Business snapshot text.", summary.NarrativeText);
        Assert.Single(summary.Highlights);
        Assert.Contains("Title i-risk", summary.Highlights);
    }

    [Fact]
    public async Task GetExecutiveSummaryAsync_RanksHighlightsByChangeMagnitude()
    {
        IReadOnlyList<AIInsightDto> insights =
        [
            BuildInsight("i-small", InsightSeverity.Trend, 2m),
            BuildInsight("i-large", InsightSeverity.Risk, -30m),
        ];
        var sut = new SummaryEngine(new StubContextProvider("Business snapshot text."), new StubInsightEngine(insights));

        var summary = await sut.GetExecutiveSummaryAsync();

        Assert.Equal("Executive Summary", summary.Title);
        Assert.StartsWith("Title i-large", summary.Highlights[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetExecutiveSummaryAsync_CapsHighlightsAtFive()
    {
        var insights = Enumerable.Range(0, 8).Select(i => BuildInsight($"i-{i}", InsightSeverity.Trend, i)).ToList();
        var sut = new SummaryEngine(new StubContextProvider("Business snapshot text."), new StubInsightEngine(insights));

        var summary = await sut.GetExecutiveSummaryAsync();

        Assert.Equal(5, summary.Highlights.Count);
    }
}
