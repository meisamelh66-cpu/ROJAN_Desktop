namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// The reusable KPI Engine - computes every <see cref="KpiType"/> for a
/// given <see cref="AnalyticsPeriod"/>, each compared against its
/// immediately preceding period of equal length via
/// <c>Domain.Reporting.TrendCalculator</c>. Powers the Analytics
/// Dashboard's KPI Cards.
/// </summary>
public interface IKpiEngineQueryService
{
    public Task<IReadOnlyList<KpiValueDto>> GetKpisAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default);
}
