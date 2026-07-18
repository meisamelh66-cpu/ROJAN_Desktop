namespace Rojan.Desktop.Application.Dashboard;

/// <summary>Application-layer shape of a KPI reading, mapped from <see cref="Rojan.Desktop.Domain.Dashboard.KpiMetric"/> by <see cref="DashboardQueryService"/>.</summary>
public sealed record KpiMetricDto(
    string Id,
    string Label,
    string Value,
    TrendDirection TrendDirection,
    double TrendPercentage);
