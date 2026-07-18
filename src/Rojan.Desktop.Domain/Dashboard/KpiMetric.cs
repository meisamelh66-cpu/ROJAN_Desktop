namespace Rojan.Desktop.Domain.Dashboard;

/// <summary>A single key-performance-indicator reading, as returned by <see cref="IDashboardRepository"/>.</summary>
public sealed record KpiMetric(
    string Id,
    string Label,
    string Value,
    TrendDirection TrendDirection,
    double TrendPercentage);
