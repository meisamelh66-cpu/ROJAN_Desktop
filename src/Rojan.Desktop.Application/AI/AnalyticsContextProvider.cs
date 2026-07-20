using System.Globalization;
using System.Text;
using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.AI;

public sealed class AnalyticsContextProvider : IAnalyticsContextProvider
{
    private readonly AppReporting.IKpiEngineQueryService _kpiEngineQueryService;

    public AnalyticsContextProvider(AppReporting.IKpiEngineQueryService kpiEngineQueryService)
    {
        _kpiEngineQueryService = kpiEngineQueryService;
    }

    public async Task<string> GetAnalyticsContextAsync(AppReporting.AnalyticsPeriod period, CancellationToken cancellationToken = default)
    {
        var kpis = await _kpiEngineQueryService.GetKpisAsync(period, cancellationToken).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"KPI trends ({period}):");
        foreach (var kpi in kpis)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {kpi.Name}: {kpi.Unit}{kpi.Value:N1} ({kpi.Trend}, {kpi.ChangePercent:N1}% vs prior period)");
        }

        return builder.ToString();
    }
}
