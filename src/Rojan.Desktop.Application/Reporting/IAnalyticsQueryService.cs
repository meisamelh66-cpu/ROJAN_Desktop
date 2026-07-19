namespace Rojan.Desktop.Application.Reporting;

/// <summary>Backs the Analytics Dashboard - the period summary for its KPI row plus a handful of charts for its Chart Area.</summary>
public interface IAnalyticsQueryService
{
    public Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ChartDefinitionDto>> GetDashboardChartsAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default);
}
