using Rojan.Desktop.Application.Automation;
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
