namespace Rojan.Desktop.Domain.AI;

/// <summary>
/// One InsightEngine finding - never persisted (see
/// <see cref="IAIRepository"/>'s own doc comment): computed fresh from
/// live cross-module data on every request, the same "compute, don't
/// cache" choice <c>Reporting.AnalyticsSummary</c> made in Phase 20.
/// </summary>
public sealed record AIInsight(
    string Id,
    InsightCategory Category,
    InsightSeverity Severity,
    string Title,
    string Description,
    decimal? MetricValue,
    decimal? ChangePercent,
    DateTimeOffset GeneratedAt);
