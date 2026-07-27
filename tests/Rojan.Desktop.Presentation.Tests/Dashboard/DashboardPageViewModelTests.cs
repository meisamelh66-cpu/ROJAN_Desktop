using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Dashboard;

public sealed class DashboardPageViewModelTests
{
    private static DashboardOverviewDto MakeOverview(int metricCount = 1, int activityCount = 1)
    {
        var metrics = Enumerable.Range(1, metricCount)
            .Select(i => new KpiMetricDto($"kpi-{i}", $"Metric {i}", "1", TrendDirection.Flat, 0))
            .ToList();
        var activity = Enumerable.Range(1, activityCount)
            .Select(i => new ActivityEntryDto($"activity-{i}", $"Event {i}", DateTimeOffset.UnixEpoch))
            .ToList();
        return new DashboardOverviewDto(metrics, activity);
    }

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<DashboardOverviewDto>();
        var queryService = new StubDashboardQueryService(_ => tcs.Task);

        var sut = new DashboardPageViewModel(queryService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsData_StateIsLoadedAndPopulatesCollections()
    {
        var overview = MakeOverview(metricCount: 2, activityCount: 3);
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(overview));

        var sut = new DashboardPageViewModel(queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(overview.KpiMetrics, sut.KpiMetrics);
        Assert.Equal(overview.RecentActivity, sut.RecentActivity);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyOverview_StateIsEmpty()
    {
        var overview = MakeOverview(metricCount: 0, activityCount: 0);
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(overview));

        var sut = new DashboardPageViewModel(queryService);

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubDashboardQueryService(
            _ => Task.FromException<DashboardOverviewDto>(new InvalidOperationException("boom")));

        var sut = new DashboardPageViewModel(queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void Constructor_QuickActions_DoesNotIncludeNewBooking()
    {
        // UX Improvements - Dashboard Layout: New Booking was promoted to its own
        // top-of-page primary action button, removed from this list so it is not
        // duplicated in two places.
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(MakeOverview()));

        var sut = new DashboardPageViewModel(queryService);

        Assert.DoesNotContain(sut.QuickActions, item => item.Label == Rojan.Desktop.Presentation.Localization.Strings.Dashboard_QuickAction_NewBooking);
        Assert.Equal(3, sut.QuickActions.Count);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var overview = MakeOverview();
        var queryService = new StubDashboardQueryService(_ => shouldFail
            ? Task.FromException<DashboardOverviewDto>(new InvalidOperationException("boom"))
            : Task.FromResult(overview));
        var sut = new DashboardPageViewModel(queryService);
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
    }
}
