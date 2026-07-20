using Rojan.Desktop.Application.AI;
using Rojan.Desktop.Application.Tests.Reporting;
using AppHr = Rojan.Desktop.Application.HR;
using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class InsightEngineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static readonly AppReporting.AnalyticsSummaryDto Summary = new(
        "This month", 10000m, 120, 80, 10, 65m, "Haircut", "Alex", 5000m, 2, 8000m, 92m, Now);

    private static InsightEngine CreateSut(
        IReadOnlyList<AppReporting.KpiValueDto>? kpis = null,
        IReadOnlyList<AppHr.CommissionTransactionDto>? commissions = null) => new(
        new StubKpiEngineQueryService(kpis ?? []),
        new StubAnalyticsQueryService(Summary),
        new StubCommissionQueryService(commissions ?? []));

    private static AppReporting.KpiValueDto BuildKpi(AppReporting.KpiType type, decimal changePercent, AppReporting.TrendDirection trend) =>
        new($"kpi-{type}", type.ToString(), type, 100m, 90m, trend, changePercent, "$", "This month");

    [Fact]
    public async Task GenerateInsightsAsync_ProducesOneInsightPerKpiPlusCommission()
    {
        IReadOnlyList<AppReporting.KpiValueDto> kpis =
        [
            BuildKpi(AppReporting.KpiType.Revenue, 15m, AppReporting.TrendDirection.Up),
            BuildKpi(AppReporting.KpiType.Appointments, 5m, AppReporting.TrendDirection.Up),
        ];
        var sut = CreateSut(kpis);

        var insights = await sut.GenerateInsightsAsync();

        Assert.Equal(3, insights.Count);
        Assert.Contains(insights, i => i.Id == "insight-commission");
    }

    [Theory]
    [InlineData(AppReporting.TrendDirection.Flat, 0, InsightSeverity.Info)]
    [InlineData(AppReporting.TrendDirection.Up, 15, InsightSeverity.Opportunity)]
    [InlineData(AppReporting.TrendDirection.Down, 15, InsightSeverity.Risk)]
    [InlineData(AppReporting.TrendDirection.Up, 3, InsightSeverity.Trend)]
    [InlineData(AppReporting.TrendDirection.Down, 3, InsightSeverity.Trend)]
    public async Task GenerateInsightsAsync_ClassifiesSeverityFromTrendAndMagnitude(
        AppReporting.TrendDirection trend, decimal changePercent, InsightSeverity expectedSeverity)
    {
        var sut = CreateSut([BuildKpi(AppReporting.KpiType.Revenue, changePercent, trend)]);

        var insights = await sut.GenerateInsightsAsync();

        var revenueInsight = insights.Single(i => i.Category == InsightCategory.Revenue);
        Assert.Equal(expectedSeverity, revenueInsight.Severity);
    }

    [Fact]
    public async Task GenerateInsightsAsync_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        IReadOnlyList<AppReporting.KpiValueDto> kpis =
        [
            BuildKpi(AppReporting.KpiType.Revenue, 15m, AppReporting.TrendDirection.Up),
            BuildKpi(AppReporting.KpiType.Inventory, 15m, AppReporting.TrendDirection.Down),
        ];
        var sut = CreateSut(kpis);

        var insights = await sut.GenerateInsightsAsync(InsightCategory.Inventory);

        Assert.All(insights, i => Assert.Equal(InsightCategory.Inventory, i.Category));
    }

    [Fact]
    public async Task GenerateInsightsAsync_CommissionInsight_ComparesCurrentVsPreviousMonth()
    {
        IReadOnlyList<AppHr.CommissionTransactionDto> transactions =
        [
            new("t1", "e1", "Jordan", "inv-1", "Haircut", 600m, 500m, Now),
            new("t2", "e1", "Jordan", "inv-2", "Haircut", 400m, 300m, Now.AddMonths(-1)),
        ];
        var sut = CreateSut(commissions: transactions);

        var insights = await sut.GenerateInsightsAsync();

        var commissionInsight = insights.Single(i => i.Id == "insight-commission");
        Assert.Equal(500m, commissionInsight.MetricValue);
    }

    [Theory]
    [InlineData(AppReporting.KpiType.Revenue, InsightCategory.Revenue)]
    [InlineData(AppReporting.KpiType.Appointments, InsightCategory.Appointment)]
    [InlineData(AppReporting.KpiType.Customers, InsightCategory.Customer)]
    [InlineData(AppReporting.KpiType.Inventory, InsightCategory.Inventory)]
    [InlineData(AppReporting.KpiType.Payroll, InsightCategory.Payroll)]
    [InlineData(AppReporting.KpiType.Attendance, InsightCategory.Attendance)]
    [InlineData(AppReporting.KpiType.Growth, InsightCategory.Customer)]
    [InlineData(AppReporting.KpiType.Trend, InsightCategory.General)]
    public async Task GenerateInsightsAsync_MapsEveryKpiTypeToAnInsightCategory(AppReporting.KpiType kpiType, InsightCategory expectedCategory)
    {
        var sut = CreateSut([BuildKpi(kpiType, 5m, AppReporting.TrendDirection.Up)]);

        var insights = await sut.GenerateInsightsAsync();

        Assert.Equal(expectedCategory, insights.Single(i => i.Id == $"insight-kpi-{kpiType}").Category);
    }
}
