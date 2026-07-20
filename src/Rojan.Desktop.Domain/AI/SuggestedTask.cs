namespace Rojan.Desktop.Domain.AI;

/// <summary>A concrete, actionable to-do the RecommendationEngine derives from a higher-priority <see cref="AIRecommendation"/> - the Action Center's checklist content.</summary>
public sealed record SuggestedTask(
    string Id,
    string Title,
    InsightCategory Category,
    RecommendationPriority Priority,
    DateOnly? SuggestedDueDate);
