using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.Tests.AI;

/// <summary>A controllable <see cref="IInsightEngine"/> for testing <c>RecommendationEngine</c>/<c>NotificationInsightService</c>/<c>SummaryEngine</c> in isolation from the KPI/Analytics/Commission plumbing <see cref="InsightEngine"/> itself needs.</summary>
internal sealed class StubInsightEngine(IReadOnlyList<AIInsightDto> insights) : IInsightEngine
{
    public Task<IReadOnlyList<AIInsightDto>> GenerateInsightsAsync(InsightCategory? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(filter is null ? insights : insights.Where(i => i.Category == filter).ToList());
}
