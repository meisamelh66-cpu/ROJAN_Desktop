using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Domain.Tests.Automation;

/// <summary>Exercises <see cref="ApprovalRules"/>'s multi-step approval state machine.</summary>
public sealed class ApprovalRulesTests
{
    private static ApprovalRequest MultiStepRequest() => new(
        "req-1", ApprovalType.Leave, "Leave Request", string.Empty, "user-1", DateTimeOffset.UtcNow,
        [
            new ApprovalStep(0, "BranchManager", ApprovalStepStatus.Pending, null, null, null),
            new ApprovalStep(1, "OrganizationManager", ApprovalStepStatus.Pending, null, null, null),
        ],
        ApprovalStatus.Pending, CurrentStepIndex: 0, WorkflowExecutionId: null, "org-1", "branch-1");

    [Fact]
    public void Decide_ApproveNonFinalStep_AdvancesToNextStepAndStaysPending()
    {
        var request = MultiStepRequest();

        var result = ApprovalRules.Decide(request, approve: true, "approver-1", "looks good", DateTimeOffset.UtcNow);

        Assert.Equal(ApprovalStatus.Pending, result.Status);
        Assert.Equal(1, result.CurrentStepIndex);
        Assert.Equal(ApprovalStepStatus.Approved, result.Steps[0].Status);
        Assert.Equal("approver-1", result.Steps[0].DecidedByUserId);
    }

    [Fact]
    public void Decide_ApproveFinalStep_ApprovesTheWholeRequest()
    {
        var request = MultiStepRequest() with { CurrentStepIndex = 1 };

        var result = ApprovalRules.Decide(request, approve: true, "approver-2", null, DateTimeOffset.UtcNow);

        Assert.Equal(ApprovalStatus.Approved, result.Status);
    }

    [Fact]
    public void Decide_RejectAtAnyStep_RejectsTheWholeRequestImmediately()
    {
        var request = MultiStepRequest();

        var result = ApprovalRules.Decide(request, approve: false, "approver-1", "not now", DateTimeOffset.UtcNow);

        Assert.Equal(ApprovalStatus.Rejected, result.Status);
        Assert.Equal(ApprovalStepStatus.Rejected, result.Steps[0].Status);
    }

    [Fact]
    public void Decide_AlreadyTerminalRequest_Throws()
    {
        var request = MultiStepRequest() with { Status = ApprovalStatus.Approved };

        Assert.Throws<InvalidOperationException>(() => ApprovalRules.Decide(request, true, "u", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CurrentStep_PendingRequest_ReturnsTheStepAtCurrentIndex()
    {
        var request = MultiStepRequest() with { CurrentStepIndex = 1 };

        Assert.Equal("OrganizationManager", ApprovalRules.CurrentStep(request)!.ApproverRole);
    }

    [Fact]
    public void CurrentStep_TerminalRequest_ReturnsNull()
    {
        var request = MultiStepRequest() with { Status = ApprovalStatus.Rejected };

        Assert.Null(ApprovalRules.CurrentStep(request));
    }

    [Theory]
    [InlineData(ApprovalStatus.Approved, true)]
    [InlineData(ApprovalStatus.Rejected, true)]
    [InlineData(ApprovalStatus.Cancelled, true)]
    [InlineData(ApprovalStatus.Pending, false)]
    public void IsTerminal_ReflectsTerminalStatuses(ApprovalStatus status, bool expected)
    {
        Assert.Equal(expected, ApprovalRules.IsTerminal(status));
    }
}
