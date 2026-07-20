using Rojan.Desktop.Application.AI;
using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.AI;

public sealed class BusinessHealthServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static AppReporting.KpiValueDto BuildKpi(AppReporting.KpiType type, decimal changePercent) =>
        new($"kpi-{type}", type.ToString(), type, 100m, 90m, AppReporting.TrendDirection.Up, changePercent, "$", "This month");

    [Fact]
    public async Task ComputeScoreAsync_ReturnsFiveWeightedComponents()
    {
        var summary = new AppReporting.AnalyticsSummaryDto("This month", 10000m, 100, 80, 10, 70m, "Haircut", "Alex", 5000m, 0, 8000m, 95m, Now);
        var sut = new BusinessHealthService(
            new StubKpiEngineQueryService([BuildKpi(AppReporting.KpiType.Revenue, 0m), BuildKpi(AppReporting.KpiType.Appointments, 0m)]),
            new StubAnalyticsQueryService(summary));

        var score = await sut.ComputeScoreAsync();

        Assert.Equal(5, score.Components.Count);
        Assert.InRange(score.OverallScore, 0m, 100m);
    }

    [Fact]
    public async Task ComputeScoreAsync_HighRetentionAndAttendanceProduceAHighOverallScore()
    {
        var summary = new AppReporting.AnalyticsSummaryDto("This month", 10000m, 100, 80, 10, 95m, "Haircut", "Alex", 5000m, 0, 8000m, 98m, Now);
        var sut = new BusinessHealthService(
            new StubKpiEngineQueryService([BuildKpi(AppReporting.KpiType.Revenue, 20m), BuildKpi(AppReporting.KpiType.Appointments, 20m)]),
            new StubAnalyticsQueryService(summary));

        var score = await sut.ComputeScoreAsync();

        Assert.True(score.OverallScore >= 70m);
    }

    [Fact]
    public async Task ComputeScoreAsync_LowStockReducesTheInventoryComponent()
    {
        var healthySummary = new AppReporting.AnalyticsSummaryDto("This month", 10000m, 100, 80, 10, 70m, "Haircut", "Alex", 5000m, 0, 8000m, 90m, Now);
        var lowStockSummary = healthySummary with { LowStockCount = 5 };
        var sut = new BusinessHealthService(
            new StubKpiEngineQueryService([BuildKpi(AppReporting.KpiType.Revenue, 0m), BuildKpi(AppReporting.KpiType.Appointments, 0m)]),
            new StubAnalyticsQueryService(lowStockSummary));
        var healthySut = new BusinessHealthService(
            new StubKpiEngineQueryService([BuildKpi(AppReporting.KpiType.Revenue, 0m), BuildKpi(AppReporting.KpiType.Appointments, 0m)]),
            new StubAnalyticsQueryService(healthySummary));

        var lowStockScore = await sut.ComputeScoreAsync();
        var healthyScore = await healthySut.ComputeScoreAsync();

        var lowStockInventory = lowStockScore.Components.Single(c => c.Category == InsightCategory.Inventory);
        var healthyInventory = healthyScore.Components.Single(c => c.Category == InsightCategory.Inventory);
        Assert.True(lowStockInventory.Score < healthyInventory.Score);
    }

    [Fact]
    public async Task ComputeScoreAsync_SummaryTextReflectsOverallScoreBand()
    {
        var summary = new AppReporting.AnalyticsSummaryDto("This month", 10000m, 100, 80, 10, 10m, "Haircut", "Alex", 5000m, 20, 8000m, 10m, Now);
        var sut = new BusinessHealthService(
            new StubKpiEngineQueryService([BuildKpi(AppReporting.KpiType.Revenue, -40m), BuildKpi(AppReporting.KpiType.Appointments, -40m)]),
            new StubAnalyticsQueryService(summary));

        var score = await sut.ComputeScoreAsync();

        Assert.Contains("needs attention", score.Summary, StringComparison.OrdinalIgnoreCase);
    }
}
