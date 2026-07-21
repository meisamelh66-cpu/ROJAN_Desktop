using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class AutomationDashboardTabViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsSummaryAndRecentExecutions()
    {
        var summary = new AutomationDashboardSummaryDto(3, 2, 10, 1, 90d, 250d, 4);
        var dashboardQuery = new StubAutomationDashboardQueryService(summary);
        var executionEngine = new StubWorkflowExecutionEngine();
        await executionEngine.ExecuteAsync("w1", null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        var sut = new AutomationDashboardTabViewModel(dashboardQuery, executionEngine);
        await sut.LoadAsync();

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(3, sut.TotalWorkflows);
        Assert.Equal(2, sut.PublishedWorkflows);
        Assert.Equal(10, sut.ExecutionsToday);
        Assert.Equal(1, sut.FailuresToday);
        Assert.Equal(90d, sut.SuccessRatePercent);
        Assert.Equal(4, sut.PendingApprovals);
        Assert.Single(sut.RecentExecutions);
    }
}
