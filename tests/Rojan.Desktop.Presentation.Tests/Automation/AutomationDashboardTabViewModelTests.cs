using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class AutomationDashboardTabViewModelTests
{
    private const string Secret = "workflow-name-SECRET-9f3";

    [Fact]
    public async Task LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var dashboardQuery = new StubAutomationDashboardQueryService(new AutomationDashboardSummaryDto(0, 0, 0, 0, 0, 0, 0))
        {
            GetSummaryException = new InvalidOperationException(Secret),
        };
        var logger = new RecordingLogger<AutomationDashboardTabViewModel>();
        var sut = new AutomationDashboardTabViewModel(dashboardQuery, new StubWorkflowExecutionEngine(), logger);

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.DoesNotContain(Secret, sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows()
    {
        var dashboardQuery = new StubAutomationDashboardQueryService(new AutomationDashboardSummaryDto(0, 0, 0, 0, 0, 0, 0))
        {
            GetSummaryException = new InvalidOperationException("boom"),
        };
        var sut = new AutomationDashboardTabViewModel(dashboardQuery, new StubWorkflowExecutionEngine());

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
    }

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
