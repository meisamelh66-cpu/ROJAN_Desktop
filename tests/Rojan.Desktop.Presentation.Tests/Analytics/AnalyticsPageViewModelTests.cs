using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Tests.Reporting;
using Rojan.Desktop.Presentation.ViewModels.Analytics;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Analytics;

public sealed class AnalyticsPageViewModelTests
{
    private static readonly IReadOnlyList<KpiValueDto> Kpis =
    [
        new("kpi-revenue", "Revenue", KpiType.Revenue, 294m, 280m, TrendDirection.Up, 5m, "$", "Today"),
    ];

    private static readonly AnalyticsSummaryDto Summary = new(
        "Today", 294m, 3, 7, 1, 71.4m, "Haircut", "Jordan Lee", 3850m, 1, 0m, 43m, DateTimeOffset.Now);

    private static readonly IReadOnlyList<ChartDefinitionDto> Charts =
    [
        new("chart-revenue-by-day", "Revenue - Last 7 Days", ChartType.Column, ["Jul 19"], [new ChartSeriesDto("Revenue", [294m])]),
    ];

    private static AnalyticsPageViewModel CreateSut() =>
        new(new StubKpiEngineQueryService(Kpis), new StubAnalyticsQueryService(Summary, Charts));

    [Fact]
    public void Constructor_LoadsKpisSummaryAndCharts()
    {
        var sut = CreateSut();

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.Kpis);
        Assert.Equal(Summary, sut.Summary);
        Assert.Single(sut.Charts);
    }

    [Fact]
    public void Constructor_DefaultsToDaily()
    {
        var sut = CreateSut();

        Assert.Equal(AnalyticsPeriod.Daily, sut.SelectedPeriod);
    }

    [Fact]
    public void SelectPeriodCommand_ChangesSelectedPeriod()
    {
        var sut = CreateSut();

        sut.SelectPeriodCommand.Execute(AnalyticsPeriod.Monthly);

        Assert.Equal(AnalyticsPeriod.Monthly, sut.SelectedPeriod);
    }

    [Fact]
    public void SelectedPeriod_SettingSameValue_DoesNotChangeState()
    {
        var sut = CreateSut();
        Assert.Equal(DashboardState.Loaded, sut.State);

        sut.SelectedPeriod = AnalyticsPeriod.Daily;

        Assert.Equal(DashboardState.Loaded, sut.State);
    }
}
