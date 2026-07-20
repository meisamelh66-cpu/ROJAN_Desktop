namespace Rojan.Desktop.Application.AI;

public sealed record SuggestedTaskDto(
    string Id,
    string Title,
    InsightCategory Category,
    RecommendationPriority Priority,
    DateOnly? SuggestedDueDate);
