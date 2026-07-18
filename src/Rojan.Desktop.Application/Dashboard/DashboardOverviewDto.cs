namespace Rojan.Desktop.Application.Dashboard;

/// <summary>Everything <c>DashboardPageViewModel</c> needs for one load - both lists fetched together as a single unit of work.</summary>
public sealed record DashboardOverviewDto(
    IReadOnlyList<KpiMetricDto> KpiMetrics,
    IReadOnlyList<ActivityEntryDto> RecentActivity);
