using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Automation;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class AutomationPageViewModelTests
{
    private static AutomationPageViewModel CreateSut() => new(
        new FakeCurrentSessionService(),
        new StubAutomationDashboardQueryService(new AutomationDashboardSummaryDto(0, 0, 0, 0, 0, 0, 0)),
        new StubWorkflowService(),
        new StubBusinessRuleService(),
        new StubScheduledJobService(),
        new StubApprovalService(),
        new StubWorkflowExecutionEngine());

    [Fact]
    public void Constructor_ForwardsEachTabLoggerToItsChild()
    {
        var workflowsLogger = new RecordingLogger<WorkflowsTabViewModel>();
        var approvalsLogger = new RecordingLogger<ApprovalsTabViewModel>();

        _ = new AutomationPageViewModel(
            new FakeCurrentSessionService(),
            new StubAutomationDashboardQueryService(new AutomationDashboardSummaryDto(0, 0, 0, 0, 0, 0, 0)),
            new StubWorkflowService { GetAllException = new InvalidOperationException("workflows boom") },
            new StubBusinessRuleService(),
            new StubScheduledJobService(),
            new StubApprovalService { GetAllException = new InvalidOperationException("approvals boom") },
            new StubWorkflowExecutionEngine(),
            workflowsLogger: workflowsLogger,
            approvalsLogger: approvalsLogger);

        var workflowsEntry = Assert.Single(workflowsLogger.Entries);
        Assert.Equal(LogLevel.Error, workflowsEntry.Level);
        Assert.Contains("Operation=LoadAsync", workflowsEntry.Message, StringComparison.Ordinal);
        var approvalsEntry = Assert.Single(approvalsLogger.Entries);
        Assert.Contains("Operation=LoadAsync", approvalsEntry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_CreatesAllFiveTabsAndLoadsThem()
    {
        var sut = CreateSut();

        Assert.NotNull(sut.Dashboard);
        Assert.NotNull(sut.Workflows);
        Assert.NotNull(sut.BusinessRules);
        Assert.NotNull(sut.ScheduledJobs);
        Assert.NotNull(sut.Approvals);
    }

    [Fact]
    public void SelectTabCommand_ParsesStringParameterIntoSelectedTabIndex()
    {
        var sut = CreateSut();

        sut.SelectTabCommand.Execute("2");

        Assert.Equal(2, sut.SelectedTabIndex);
    }

    [Fact]
    public void SelectTabCommand_NonNumericParameter_LeavesIndexUnchanged()
    {
        var sut = CreateSut();
        sut.SelectTabCommand.Execute("1");

        sut.SelectTabCommand.Execute("not-a-number");

        Assert.Equal(1, sut.SelectedTabIndex);
    }
}
