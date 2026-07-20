using AppReporting = Rojan.Desktop.Application.Reporting;
using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.AI;

public sealed class BusinessHealthService : IBusinessHealthService
{
    private readonly AppReporting.IKpiEngineQueryService _kpiEngineQueryService;
    private readonly AppReporting.IAnalyticsQueryService _analyticsQueryService;

    public BusinessHealthService(AppReporting.IKpiEngineQueryService kpiEngineQueryService, AppReporting.IAnalyticsQueryService analyticsQueryService)
    {
        _kpiEngineQueryService = kpiEngineQueryService;
        _analyticsQueryService = analyticsQueryService;
    }

    public async Task<BusinessHealthScoreDto> ComputeScoreAsync(CancellationToken cancellationToken = default)
    {
        var kpis = await _kpiEngineQueryService.GetKpisAsync(AppReporting.AnalyticsPeriod.Monthly, cancellationToken).ConfigureAwait(false);
        var summary = await _analyticsQueryService.GetAnalyticsSummaryAsync(AppReporting.AnalyticsPeriod.Monthly, cancellationToken).ConfigureAwait(false);

        var revenue = kpis.FirstOrDefault(k => k.KpiType == AppReporting.KpiType.Revenue);
        var appointments = kpis.FirstOrDefault(k => k.KpiType == AppReporting.KpiType.Appointments);
        var payroll = kpis.FirstOrDefault(k => k.KpiType == AppReporting.KpiType.Payroll);

        var components = new List<DomainAI.BusinessHealthComponent>
        {
            new(DomainAI.InsightCategory.Revenue, "Revenue trend", TrendToScore(revenue?.ChangePercent ?? 0m), 0.30m),
            new(DomainAI.InsightCategory.Appointment, "Appointment volume", TrendToScore(appointments?.ChangePercent ?? 0m), 0.20m),
            new(DomainAI.InsightCategory.Customer, "Customer retention", DomainAI.BusinessHealthCalculator.NormalizeToScore(summary.RetentionRatePercent), 0.20m),
            new(DomainAI.InsightCategory.Attendance, "Staff attendance", DomainAI.BusinessHealthCalculator.NormalizeToScore(summary.AttendanceRatePercent), 0.15m),
            new(DomainAI.InsightCategory.Inventory, "Inventory health", summary.LowStockCount == 0 ? 100m : DomainAI.BusinessHealthCalculator.NormalizeToScore(100m - (summary.LowStockCount * 10m)), 0.15m),
        };

        var overallScore = DomainAI.BusinessHealthCalculator.ComputeOverallScore(components);
        var summaryText = BuildSummary(overallScore);

        var domainScore = new DomainAI.BusinessHealthScore(overallScore, components, summaryText, DateTimeOffset.Now);
        return AIMapper.MapHealthScore(domainScore);
    }

    /// <summary>Maps a trend's percent change onto a 0-100 score, centered at 50 (no change) - a real, deterministic, if simple, translation from "relative movement" to "absolute health," clamped by <c>BusinessHealthCalculator.NormalizeToScore</c>.</summary>
    private static decimal TrendToScore(decimal changePercent) => DomainAI.BusinessHealthCalculator.NormalizeToScore(50m + changePercent);

    private static string BuildSummary(decimal overallScore) => overallScore switch
    {
        >= 80m => "Business health is strong across the board.",
        >= 60m => "Business health is solid, with some areas worth watching.",
        >= 40m => "Business health is mixed - review the components below.",
        _ => "Business health needs attention - several components are underperforming.",
    };
}
