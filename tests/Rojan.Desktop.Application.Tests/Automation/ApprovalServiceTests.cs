using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Exercises <see cref="ApprovalService"/> - Requirement 32.5's multi-step approvals, including the seam where a workflow-originated request's decision resumes/fails its paused <c>WorkflowExecution</c>.</summary>
public sealed class ApprovalServiceTests
{
    [Fact]
    public async Task CreateAsync_NoApproverRoles_Throws()
    {
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new ApprovalService(new FakeApprovalRepository(), engine);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(ApprovalType.Leave, "Leave Request", "", [], "user-1", "org-1", "branch-1"));
    }

    [Fact]
    public async Task CreateAsync_MultipleApproverRoles_StartsPendingAtFirstStep()
    {
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new ApprovalService(new FakeApprovalRepository(), engine);

        var request = await service.CreateAsync(ApprovalType.Leave, "Leave Request", "", ["Supervisor", "HR"], "user-1", "org-1", "branch-1");

        Assert.Equal(ApprovalStatus.Pending, request.Status);
        Assert.Equal(0, request.CurrentStepIndex);
        Assert.Equal(2, request.Steps.Count);
    }

    [Fact]
    public async Task DecideAsync_ApproveNonFinalStep_AdvancesToNextStepStillPending()
    {
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new ApprovalService(new FakeApprovalRepository(), engine);
        var request = await service.CreateAsync(ApprovalType.Leave, "Leave Request", "", ["Supervisor", "HR"], "user-1", "org-1", "branch-1");

        var decided = await service.DecideAsync(request.Id, approve: true, "supervisor-1", null);

        Assert.Equal(ApprovalStatus.Pending, decided.Status);
        Assert.Equal(1, decided.CurrentStepIndex);
    }

    [Fact]
    public async Task DecideAsync_ApproveFinalStep_ApprovesTheWholeRequest()
    {
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new ApprovalService(new FakeApprovalRepository(), engine);
        var request = await service.CreateAsync(ApprovalType.Expense, "Expense Approval", "", ["Manager"], "user-1", "org-1", "branch-1");

        var decided = await service.DecideAsync(request.Id, approve: true, "manager-1", "looks good");

        Assert.Equal(ApprovalStatus.Approved, decided.Status);
    }

    [Fact]
    public async Task DecideAsync_RejectAtAnyStep_RejectsTheWholeRequest()
    {
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new ApprovalService(new FakeApprovalRepository(), engine);
        var request = await service.CreateAsync(ApprovalType.Inventory, "Inventory Approval", "", ["Supervisor", "HR"], "user-1", "org-1", "branch-1");

        var decided = await service.DecideAsync(request.Id, approve: false, "supervisor-1", "denied");

        Assert.Equal(ApprovalStatus.Rejected, decided.Status);
    }

    [Fact]
    public async Task DecideAsync_ApprovedWorkflowOriginatedRequest_ResumesThePausedExecution()
    {
        var workflows = new FakeWorkflowRepository();
        var executions = new FakeWorkflowExecutionRepository();
        var approvals = new FakeApprovalRepository();
        var workflowService = new WorkflowService(workflows);
        var startId = Guid.NewGuid().ToString("N");
        var approvalId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var draft = await workflowService.CreateDraftAsync(
            "Branch Approval Flow", "",
            [AutomationTestFactory.Step(startId, WorkflowStepType.Start, approvalId), AutomationTestFactory.Step(approvalId, WorkflowStepType.Approval, endId), AutomationTestFactory.Step(endId, WorkflowStepType.End)],
            [], "user-1", "org-1", "branch-1");
        var published = await workflowService.PublishAsync(draft.Id);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions, approvals);
        var paused = await engine.ExecuteAsync(published.Id, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");
        var approvalRequest = (await approvals.GetAllAsync()).Single(request => request.WorkflowExecutionId == paused.Id);
        var service = new ApprovalService(approvals, engine);

        await service.DecideAsync(approvalRequest.Id, approve: true, "approver-1", null);

        var resumedExecution = await engine.GetByIdAsync(paused.Id);
        Assert.Equal(WorkflowExecutionStatus.Completed, resumedExecution!.Status);
    }

    [Fact]
    public async Task GetPendingForRoleAsync_ReturnsOnlyRequestsCurrentlyAwaitingThatRole()
    {
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var service = new ApprovalService(new FakeApprovalRepository(), engine);
        var supervisorFirst = await service.CreateAsync(ApprovalType.Leave, "Leave A", "", ["Supervisor", "HR"], "user-1", "org-1", "branch-1");
        await service.CreateAsync(ApprovalType.Leave, "Leave B", "", ["HR"], "user-2", "org-1", "branch-1");

        var pendingForSupervisor = await service.GetPendingForRoleAsync("Supervisor");

        Assert.Single(pendingForSupervisor);
        Assert.Equal(supervisorFirst.Id, pendingForSupervisor[0].Id);
    }
}
