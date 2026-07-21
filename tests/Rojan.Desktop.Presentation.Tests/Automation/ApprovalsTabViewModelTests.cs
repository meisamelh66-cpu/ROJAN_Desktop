using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class ApprovalsTabViewModelTests
{
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
