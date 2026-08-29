using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Reporting;
using Rojan.Desktop.Presentation.Tests.Specialists;
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

    // Phase 8.23 Logging Wave 2B: LoadAsync now logs at Error before surfacing the
    // Error state - user-visible behaviour (State / ErrorMessage) is unchanged.

    [Fact]
    public void LoadAsync_QueryThrows_LogsError()
    {
        var logger = new RecordingLogger<AnalyticsPageViewModel>();

        var sut = new AnalyticsPageViewModel(new ThrowingKpiEngineQueryService(), new StubAnalyticsQueryService(Summary, Charts), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var exception = Record.Exception(() =>
            new AnalyticsPageViewModel(new ThrowingKpiEngineQueryService(), new StubAnalyticsQueryService(Summary, Charts)));

        Assert.Null(exception);
    }

    private sealed class ThrowingKpiEngineQueryService : IKpiEngineQueryService
    {
        public Task<IReadOnlyList<KpiValueDto>> GetKpisAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<KpiValueDto>>(new InvalidOperationException("boom"));
    }
}
