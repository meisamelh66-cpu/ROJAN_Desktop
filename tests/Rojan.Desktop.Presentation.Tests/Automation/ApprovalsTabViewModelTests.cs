using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class ApprovalsTabViewModelTests
{
    private const string Secret = "approval-comment-SECRET-payroll";

    [Fact]
    public async Task LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var approvals = new StubApprovalService { GetAllException = new InvalidOperationException(Secret) };
        var logger = new RecordingLogger<ApprovalsTabViewModel>();
        var sut = new ApprovalsTabViewModel(approvals, "manager-1", logger);

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DecideAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var approvals = new StubApprovalService();
        approvals.Seed(new ApprovalRequestDto(
            "a1", ApprovalType.Leave, "Leave Request", "", "user-1", DateTimeOffset.UtcNow,
            [new ApprovalStepDto(0, "Manager", ApprovalStepStatus.Pending, null, null, null)],
            ApprovalStatus.Pending, 0, null, "org-1", "branch-1"));
        var logger = new RecordingLogger<ApprovalsTabViewModel>();
        var sut = new ApprovalsTabViewModel(approvals, "manager-1", logger);
        sut.LoadCommand.Execute(null);
        approvals.DecideException = new InvalidOperationException(Secret);

        sut.ApproveCommand.Execute(sut.Requests[0]);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=DecideAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows()
    {
        var approvals = new StubApprovalService { GetAllException = new InvalidOperationException("boom") };
        var sut = new ApprovalsTabViewModel(approvals, "manager-1");

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
    }

    private static (ApprovalsTabViewModel Sut, StubApprovalService Approvals) CreateSut()
    {
        var approvals = new StubApprovalService();
        approvals.Seed(new ApprovalRequestDto(
            "a1", ApprovalType.Leave, "Leave Request", "", "user-1", DateTimeOffset.UtcNow,
            [new ApprovalStepDto(0, "Manager", ApprovalStepStatus.Pending, null, null, null)],
            ApprovalStatus.Pending, 0, null, "org-1", "branch-1"));
        var sut = new ApprovalsTabViewModel(approvals, "manager-1");
        sut.LoadCommand.Execute(null);
        return (sut, approvals);
    }

    [Fact]
    public void LoadCommand_LoadsPendingRequests()
    {
        var (sut, _) = CreateSut();

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.Requests);
        Assert.Equal(ApprovalStatus.Pending, sut.Requests[0].Status);
    }

    [Fact]
    public void ApproveCommand_MarksTheRequestApprovedAndClearsTheComment()
    {
        var (sut, approvals) = CreateSut();
        var request = sut.Requests[0];
        sut.DecisionComment = "Looks good";

        sut.ApproveCommand.Execute(request);

        Assert.Equal(ApprovalStatus.Approved, sut.Requests[0].Status);
        Assert.Equal(request.Id, approvals.LastDecidedRequestId);
        Assert.Equal("Looks good", approvals.LastDecisionComment);
        Assert.Equal(string.Empty, sut.DecisionComment);
    }

    [Fact]
    public void RejectCommand_MarksTheRequestRejected()
    {
        var (sut, _) = CreateSut();
        var request = sut.Requests[0];

        sut.RejectCommand.Execute(request);

        Assert.Equal(ApprovalStatus.Rejected, sut.Requests[0].Status);
    }
}
