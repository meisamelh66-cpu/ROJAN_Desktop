using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.AI;

/// <summary>The Prompt System's Analytics Context block - KPI trend detail from <c>Reporting.IKpiEngineQueryService</c>, distinct from <see cref="IContextProvider"/>'s broader business snapshot.</summary>
public interface IAnalyticsContextProvider
{
    public Task<string> GetAnalyticsContextAsync(AppReporting.AnalyticsPeriod period, CancellationToken cancellationToken = default);
}
