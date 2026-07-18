namespace Rojan.Desktop.Domain.Dashboard;

/// <summary>
/// Repository abstraction for dashboard data. Domain defines the contract;
/// Infrastructure provides the concrete implementation (a fake/in-memory
/// one for now - Phase 06B explicitly has no backend integration yet).
/// </summary>
public interface IDashboardRepository
{
    public Task<IReadOnlyList<KpiMetric>> GetKpiMetricsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(CancellationToken cancellationToken = default);
}
