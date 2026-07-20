namespace Rojan.Desktop.Application.AI;

public sealed record AIRecommendationDto(
    string Id,
    InsightCategory Category,
    RecommendationPriority Priority,
    string Title,
    string Description,
    string SuggestedAction);
