using System.Globalization;

namespace Rojan.Desktop.Application.AI;

public sealed class RecommendationEngine : IRecommendationEngine
{
    private readonly IInsightEngine _insightEngine;

    public RecommendationEngine(IInsightEngine insightEngine)
    {
        _insightEngine = insightEngine;
    }

    public async Task<IReadOnlyList<AIRecommendationDto>> GenerateRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        var insights = await _insightEngine.GenerateInsightsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return insights
            .Where(i => i.Severity is InsightSeverity.Risk or InsightSeverity.Opportunity or InsightSeverity.Critical)
            .Select(BuildRecommendation)
            .ToList();
    }

    public async Task<IReadOnlyList<SuggestedTaskDto>> GenerateSuggestedTasksAsync(CancellationToken cancellationToken = default)
    {
        var recommendations = await GenerateRecommendationsAsync(cancellationToken).ConfigureAwait(false);

        return recommendations
            .Where(r => r.Priority is RecommendationPriority.High or RecommendationPriority.Urgent)
            .Select(BuildSuggestedTask)
            .ToList();
    }

    private static AIRecommendationDto BuildRecommendation(AIInsightDto insight)
    {
        var priority = MapSeverityToPriority(insight.Severity);
        var action = insight.Severity switch
        {
            InsightSeverity.Risk or InsightSeverity.Critical => $"Review {insight.Title} and address the underlying cause before it worsens.",
            InsightSeverity.Opportunity => $"Capitalize on {insight.Title} while the trend holds.",
            _ => $"Review {insight.Title}.",
        };

        return new AIRecommendationDto(
            string.Create(CultureInfo.InvariantCulture, $"rec-{insight.Id}"),
            insight.Category,
            priority,
            insight.Title,
            insight.Description,
            action);
    }

    private static SuggestedTaskDto BuildSuggestedTask(AIRecommendationDto recommendation)
    {
        var daysAhead = recommendation.Priority == RecommendationPriority.Urgent ? 1 : 3;
        var dueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysAhead));

        return new SuggestedTaskDto(
            string.Create(CultureInfo.InvariantCulture, $"task-{recommendation.Id}"),
            recommendation.SuggestedAction,
            recommendation.Category,
            recommendation.Priority,
            dueDate);
    }

    private static RecommendationPriority MapSeverityToPriority(InsightSeverity severity) => severity switch
    {
        InsightSeverity.Critical => RecommendationPriority.Urgent,
        InsightSeverity.Risk => RecommendationPriority.High,
        InsightSeverity.Opportunity => RecommendationPriority.Medium,
        _ => RecommendationPriority.Low,
    };
}
