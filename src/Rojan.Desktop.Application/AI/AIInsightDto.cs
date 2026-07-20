namespace Rojan.Desktop.Application.AI;

public sealed record AIInsightDto(
    string Id,
    InsightCategory Category,
    InsightSeverity Severity,
    string Title,
    string Description,
    decimal? MetricValue,
    decimal? ChangePercent,
    DateTimeOffset GeneratedAt);
