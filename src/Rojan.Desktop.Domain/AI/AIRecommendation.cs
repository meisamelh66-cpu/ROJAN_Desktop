namespace Rojan.Desktop.Domain.AI;

/// <summary>One RecommendationEngine suggestion - the Recommendations Panel/Action Center's content. Computed fresh, never persisted, same reasoning as <see cref="AIInsight"/>.</summary>
public sealed record AIRecommendation(
    string Id,
    InsightCategory Category,
    RecommendationPriority Priority,
    string Title,
    string Description,
    string SuggestedAction);
