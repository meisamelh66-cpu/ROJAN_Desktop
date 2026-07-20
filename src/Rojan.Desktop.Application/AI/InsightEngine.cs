using System.Globalization;
using AppHr = Rojan.Desktop.Application.HR;
using AppReporting = Rojan.Desktop.Application.Reporting;
using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.AI;

public sealed class InsightEngine : IInsightEngine
{
    private readonly AppReporting.IKpiEngineQueryService _kpiEngineQueryService;
    private readonly AppReporting.IAnalyticsQueryService _analyticsQueryService;
    private readonly AppHr.ICommissionQueryService _commissionQueryService;

    public InsightEngine(
        AppReporting.IKpiEngineQueryService kpiEngineQueryService,
        AppReporting.IAnalyticsQueryService analyticsQueryService,
        AppHr.ICommissionQueryService commissionQueryService)
    {
        _kpiEngineQueryService = kpiEngineQueryService;
        _analyticsQueryService = analyticsQueryService;
        _commissionQueryService = commissionQueryService;
    }

    public async Task<IReadOnlyList<AIInsightDto>> GenerateInsightsAsync(InsightCategory? filter = null, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        var kpis = await _kpiEngineQueryService.GetKpisAsync(AppReporting.AnalyticsPeriod.Monthly, cancellationToken).ConfigureAwait(false);
        var summary = await _analyticsQueryService.GetAnalyticsSummaryAsync(AppReporting.AnalyticsPeriod.Monthly, cancellationToken).ConfigureAwait(false);

        var insights = new List<AIInsightDto>();
        foreach (var kpi in kpis)
        {
            insights.Add(BuildKpiInsight(kpi, summary));
        }

        insights.Add(await BuildCommissionInsightAsync(now, cancellationToken).ConfigureAwait(false));

        return filter is null ? insights : insights.Where(i => i.Category == filter).ToList();
    }

    private static AIInsightDto BuildKpiInsight(AppReporting.KpiValueDto kpi, AppReporting.AnalyticsSummaryDto summary)
    {
        var category = MapKpiTypeToCategory(kpi.KpiType);
        var severity = ClassifySeverity(kpi.Trend, kpi.ChangePercent);
        var title = string.Create(CultureInfo.InvariantCulture, $"{kpi.Name}: {kpi.Unit}{kpi.Value:N1} ({(kpi.ChangePercent >= 0 ? "+" : string.Empty)}{kpi.ChangePercent:N1}%)");
        var description = BuildDescription(kpi, summary, severity);

        return new AIInsightDto($"insight-{kpi.KpiDefinitionId}", category, severity, title, description, kpi.Value, kpi.ChangePercent, DateTimeOffset.Now);
    }

    private static string BuildDescription(AppReporting.KpiValueDto kpi, AppReporting.AnalyticsSummaryDto summary, InsightSeverity severity)
    {
        var direction = kpi.Trend switch
        {
            AppReporting.TrendDirection.Up => "trending up",
            AppReporting.TrendDirection.Down => "trending down",
            _ => "holding steady",
        };

        var extra = kpi.KpiType switch
        {
            AppReporting.KpiType.Revenue or AppReporting.KpiType.Trend => $" Top service this period is {summary.TopServiceName}, top specialist is {summary.TopSpecialistName}.",
            AppReporting.KpiType.Inventory => summary.LowStockCount > 0 ? $" {summary.LowStockCount} product(s) are at or below their reorder threshold." : string.Empty,
            AppReporting.KpiType.Customers or AppReporting.KpiType.Growth => $" Retention rate is {summary.RetentionRatePercent:N1}%.",
            _ => string.Empty,
        };

        return string.Create(CultureInfo.InvariantCulture, $"{kpi.Name} is {direction} versus the prior period ({kpi.ChangePercent:N1}%).{extra}");
    }

    private async Task<AIInsightDto> BuildCommissionInsightAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var transactions = await _commissionQueryService.GetAllCommissionTransactionsAsync(cancellationToken).ConfigureAwait(false);
        var currentMonthTotal = transactions.Where(t => t.EarnedAt.Month == now.Month && t.EarnedAt.Year == now.Year).Sum(t => t.CommissionAmount);
        var previousMonth = now.AddMonths(-1);
        var previousMonthTotal = transactions.Where(t => t.EarnedAt.Month == previousMonth.Month && t.EarnedAt.Year == previousMonth.Year).Sum(t => t.CommissionAmount);

        var trend = DomainReporting.TrendCalculator.ComputeTrend(currentMonthTotal, previousMonthTotal);
        var changePercent = DomainReporting.TrendCalculator.ComputeChangePercent(currentMonthTotal, previousMonthTotal);
        var severity = ClassifySeverity(trend switch
        {
            DomainReporting.TrendDirection.Up => AppReporting.TrendDirection.Up,
            DomainReporting.TrendDirection.Down => AppReporting.TrendDirection.Down,
            _ => AppReporting.TrendDirection.Flat,
        }, changePercent);

        var title = string.Create(CultureInfo.InvariantCulture, $"Commission: ${currentMonthTotal:N2} this month");
        var description = string.Create(CultureInfo.InvariantCulture, $"Total commission earned across all specialists this month is ${currentMonthTotal:N2}, versus ${previousMonthTotal:N2} last month ({changePercent:N1}%).");

        return new AIInsightDto("insight-commission", InsightCategory.Commission, severity, title, description, currentMonthTotal, changePercent, DateTimeOffset.Now);
    }

    private static InsightSeverity ClassifySeverity(AppReporting.TrendDirection trend, decimal changePercent)
    {
        var magnitude = Math.Abs(changePercent);
        return trend switch
        {
            AppReporting.TrendDirection.Flat => InsightSeverity.Info,
            AppReporting.TrendDirection.Up when magnitude >= 10m => InsightSeverity.Opportunity,
            AppReporting.TrendDirection.Down when magnitude >= 10m => InsightSeverity.Risk,
            _ => InsightSeverity.Trend,
        };
    }

    private static InsightCategory MapKpiTypeToCategory(AppReporting.KpiType kpiType) => kpiType switch
    {
        AppReporting.KpiType.Revenue => InsightCategory.Revenue,
        AppReporting.KpiType.Appointments => InsightCategory.Appointment,
        AppReporting.KpiType.Customers => InsightCategory.Customer,
        AppReporting.KpiType.Inventory => InsightCategory.Inventory,
        AppReporting.KpiType.Payroll => InsightCategory.Payroll,
        AppReporting.KpiType.Attendance => InsightCategory.Attendance,
        AppReporting.KpiType.Growth => InsightCategory.Customer,
        AppReporting.KpiType.Trend => InsightCategory.General,
        _ => InsightCategory.General,
    };
}
