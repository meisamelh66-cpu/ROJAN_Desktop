using DomainDashboard = Rojan.Desktop.Domain.Dashboard;

namespace Rojan.Desktop.Application.Dashboard;

/// <summary>
/// Default <see cref="IDashboardQueryService"/> implementation - fetches
/// from <see cref="DomainDashboard.IDashboardRepository"/> (Application is
/// allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent, so nothing Domain-shaped ever crosses into
/// Presentation.
/// </summary>
public sealed class DashboardQueryService : IDashboardQueryService
{
    private readonly DomainDashboard.IDashboardRepository _repository;

    public DashboardQueryService(DomainDashboard.IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _repository.GetKpiMetricsAsync(cancellationToken).ConfigureAwait(true);
        var activity = await _repository.GetRecentActivityAsync(cancellationToken).ConfigureAwait(true);

        return new DashboardOverviewDto(
            metrics.Select(MapMetric).ToList(),
            activity.Select(MapActivity).ToList());
    }

    private static KpiMetricDto MapMetric(DomainDashboard.KpiMetric metric) =>
        new(metric.Id, metric.Label, metric.Value, MapTrendDirection(metric.TrendDirection), metric.TrendPercentage);

    private static ActivityEntryDto MapActivity(DomainDashboard.ActivityEntry activity) =>
        new(activity.Id, activity.Description, activity.OccurredAt);

    private static TrendDirection MapTrendDirection(DomainDashboard.TrendDirection direction) => direction switch
    {
        DomainDashboard.TrendDirection.Up => TrendDirection.Up,
        DomainDashboard.TrendDirection.Down => TrendDirection.Down,
        DomainDashboard.TrendDirection.Flat => TrendDirection.Flat,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown domain trend direction."),
    };
}
