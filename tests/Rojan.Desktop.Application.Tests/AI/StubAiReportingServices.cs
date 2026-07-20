using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.AI;

/// <summary>Fakes the two Reporting query services <c>Application.AI</c> composes (<see cref="AppReporting.IKpiEngineQueryService"/>/<see cref="AppReporting.IAnalyticsQueryService"/>) - same "stub the sibling interface, not its Infrastructure repository" shape as <c>Reporting.StubQueryServices</c>.</summary>
internal sealed class StubKpiEngineQueryService(IReadOnlyList<AppReporting.KpiValueDto> kpis) : AppReporting.IKpiEngineQueryService
{
    public Task<IReadOnlyList<AppReporting.KpiValueDto>> GetKpisAsync(AppReporting.AnalyticsPeriod period, CancellationToken cancellationToken = default) =>
        Task.FromResult(kpis);
}

internal sealed class StubAnalyticsQueryService(AppReporting.AnalyticsSummaryDto summary) : AppReporting.IAnalyticsQueryService
{
    public Task<AppReporting.AnalyticsSummaryDto> GetAnalyticsSummaryAsync(AppReporting.AnalyticsPeriod period, CancellationToken cancellationToken = default) =>
        Task.FromResult(summary);

    public Task<IReadOnlyList<AppReporting.ChartDefinitionDto>> GetDashboardChartsAsync(AppReporting.AnalyticsPeriod period, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppReporting.ChartDefinitionDto>>([]);
}
