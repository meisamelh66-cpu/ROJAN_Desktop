using Rojan.Desktop.Domain.Dashboard;

namespace Rojan.Desktop.Application.Tests.Dashboard;

/// <summary>Configurable <see cref="IDashboardRepository"/> test double - hands back exactly what each test configures, no hidden behavior.</summary>
internal sealed class StubDashboardRepository : IDashboardRepository
{
    private readonly IReadOnlyList<KpiMetric> _metrics;
    private readonly IReadOnlyList<ActivityEntry> _activity;

    public StubDashboardRepository(IReadOnlyList<KpiMetric> metrics, IReadOnlyList<ActivityEntry> activity)
    {
        _metrics = metrics;
        _activity = activity;
    }

    public Task<IReadOnlyList<KpiMetric>> GetKpiMetricsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_metrics);

    public Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_activity);
}
