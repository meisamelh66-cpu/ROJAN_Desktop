using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.Tests.Specialists;
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

    private static DashboardPageViewModel CreateSut(StubDashboardQueryService queryService, WorkspaceRole role = WorkspaceRole.PlatformOwner, RecordingLogger<DashboardPageViewModel>? logger = null) =>
        new(queryService, new PermissionEngine(), new FakeCurrentSessionService { CurrentRole = role }, logger);

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<DashboardOverviewDto>();
        var queryService = new StubDashboardQueryService(_ => tcs.Task);

        var sut = CreateSut(queryService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsData_StateIsLoadedAndPopulatesCollections()
    {
        var overview = MakeOverview(metricCount: 2, activityCount: 3);
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(overview));

        var sut = CreateSut(queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(overview.KpiMetrics, sut.KpiMetrics);
        Assert.Equal(overview.RecentActivity, sut.RecentActivity);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyOverview_StateIsEmpty()
    {
        var overview = MakeOverview(metricCount: 0, activityCount: 0);
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(overview));

        var sut = CreateSut(queryService);

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubDashboardQueryService(
            _ => Task.FromException<DashboardOverviewDto>(new InvalidOperationException("boom")));

        var sut = CreateSut(queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_LogsError_OperationNameOnly_NoExceptionLeak()
    {
        const string backendBody = "HTTP 500: backend response body / dashboard secret";
        var queryService = new StubDashboardQueryService(
            _ => Task.FromException<DashboardOverviewDto>(new InvalidOperationException(backendBody)));
        var logger = new RecordingLogger<DashboardPageViewModel>();

        var sut = CreateSut(queryService, logger: logger);

        // User-visible behaviour unchanged - the log is additive.
        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(backendBody, sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=LoadAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(backendBody, StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubDashboardQueryService(
            _ => Task.FromException<DashboardOverviewDto>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() => CreateSut(queryService));

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_QuickActions_DoesNotIncludeNewBooking()
    {
        // UX Improvements - Dashboard Layout: New Booking was promoted to its own
        // top-of-page primary action button, removed from this list so it is not
        // duplicated in two places.
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(MakeOverview()));

        var sut = CreateSut(queryService);

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
        var sut = CreateSut(queryService);
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
    }

    // ---- Reception Stabilization Sprint: financial KPI RBAC ----

    private static DashboardOverviewDto MakeOverviewWithRevenueKpi() => new(
        [
            new KpiMetricDto("kpi-bookings", "Bookings", "128", TrendDirection.Up, 12.5),
            new KpiMetricDto("kpi-revenue", "Revenue", "124,000,000", TrendDirection.Down, 2.1),
        ],
        []);

    [Fact]
    public void LoadAsync_RoleWithoutAccountingView_ExcludesRevenueKpi()
    {
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(MakeOverviewWithRevenueKpi()));

        var sut = CreateSut(queryService, WorkspaceRole.Reception);

        Assert.DoesNotContain(sut.KpiMetrics, metric => metric.Id == "kpi-revenue");
        Assert.Contains(sut.KpiMetrics, metric => metric.Id == "kpi-bookings");
    }

    [Fact]
    public void LoadAsync_RoleWithAccountingView_IncludesRevenueKpi()
    {
        var queryService = new StubDashboardQueryService(_ => Task.FromResult(MakeOverviewWithRevenueKpi()));

        var sut = CreateSut(queryService, WorkspaceRole.PlatformOwner);

        Assert.Contains(sut.KpiMetrics, metric => metric.Id == "kpi-revenue");
    }

    private sealed class FakeCurrentSessionService : ICurrentSessionService
    {
        public OrganizationDto? CurrentOrganization => null;

        public BranchDto? CurrentBranch => null;

        public WorkspaceRole CurrentRole { get; init; } = WorkspaceRole.PlatformOwner;

        public DesktopContextState ContextState => DesktopContextState.NoBusinessContext;

        public IReadOnlyList<BranchDto> AvailableBranches => [];

        public IReadOnlyList<string> RecentBranchIds => [];

        public IReadOnlyList<string> FavoriteBranchIds => [];

        public event EventHandler? SessionChanged { add { } remove { } }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
