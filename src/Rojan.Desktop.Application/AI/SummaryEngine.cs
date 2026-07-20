namespace Rojan.Desktop.Application.AI;

public sealed class SummaryEngine : ISummaryEngine
{
    private readonly IContextProvider _contextProvider;
    private readonly IInsightEngine _insightEngine;

    public SummaryEngine(IContextProvider contextProvider, IInsightEngine insightEngine)
    {
        _contextProvider = contextProvider;
        _insightEngine = insightEngine;
    }

    public async Task<BusinessSummaryDto> GetDailySummaryAsync(CancellationToken cancellationToken = default)
    {
        var businessContext = await _contextProvider.GetBusinessContextAsync(cancellationToken).ConfigureAwait(false);
        var insights = await _insightEngine.GenerateInsightsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var highlights = insights
            .Where(i => i.Severity is InsightSeverity.Risk or InsightSeverity.Opportunity or InsightSeverity.Critical)
            .Select(i => i.Title)
            .ToList();

        return new BusinessSummaryDto("Daily Summary", businessContext, highlights, DateTimeOffset.Now);
    }

    public async Task<BusinessSummaryDto> GetExecutiveSummaryAsync(CancellationToken cancellationToken = default)
    {
        var businessContext = await _contextProvider.GetBusinessContextAsync(cancellationToken).ConfigureAwait(false);
        var insights = await _insightEngine.GenerateInsightsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var highlights = insights
            .OrderByDescending(i => Math.Abs(i.ChangePercent ?? 0m))
            .Take(5)
            .Select(i => $"{i.Title} - {i.Description}")
            .ToList();

        return new BusinessSummaryDto("Executive Summary", businessContext, highlights, DateTimeOffset.Now);
    }
}
