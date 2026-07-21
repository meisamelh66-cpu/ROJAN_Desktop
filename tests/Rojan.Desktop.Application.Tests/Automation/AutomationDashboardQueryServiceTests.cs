using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Exercises <see cref="AutomationDashboardQueryService"/> - Requirement 32.12's live-aggregated workflow count, today's executions/failures/success rate/average duration, and pending approvals.</summary>
public sealed class AutomationDashboardQueryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_EmptyRepositories_ReturnsAllZeroes()
    {
        var service = new AutomationDashboardQueryService(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository(), new FakeApprovalRepository());

        var summary = await service.GetSummaryAsync();

        Assert.Equal(0, summary.TotalWorkflows);
        Assert.Equal(0, summary.PublishedWorkflows);
        Assert.Equal(0, summary.ExecutionsToday);
        Assert.Equal(0, summary.FailuresToday);
        Assert.Equal(0d, summary.SuccessRatePercent);
        Assert.Equal(0, summary.PendingApprovals);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsWorkflowLineagesNotIndividualVersions()
    {
        var workflows = new FakeWorkflowRepository();
        var workflowService = new WorkflowService(workflows);
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var draft = await workflowService.CreateDraftAsync("Flow", "", [AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId), AutomationTestFactory.Step(endId, WorkflowStepType.End)], [], "user-1", "org-1", "branch-1");
        var published = await workflowService.PublishAsync(draft.Id);
        await workflowService.RollbackAsync(published.ParentWorkflowId, published.Version, "user-1");
        var service = new AutomationDashboardQueryService(workflows, new FakeWorkflowExecutionRepository(), new FakeApprovalRepository());

        var summary = await service.GetSummaryAsync();

        Assert.Equal(1, summary.TotalWorkflows);
        Assert.Equal(1, summary.PublishedWorkflows);
    }

    [Fact]
    public async Task GetSummaryAsync_ComputesSuccessRateAndFailuresFromTodaysExecutionsOnly()
    {
        var workflows = new FakeWorkflowRepository();
        var executions = new FakeWorkflowExecutionRepository();
        var workflowService = new WorkflowService(workflows);
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var draft = await workflowService.CreateDraftAsync("Flow", "", [AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId), AutomationTestFactory.Step(endId, WorkflowStepType.End)], [], "user-1", "org-1", "branch-1");
        var published = await workflowService.PublishAsync(draft.Id);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);
        await engine.ExecuteAsync(published.Id, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");
        await engine.ExecuteAsync(published.Id, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");
        var service = new AutomationDashboardQueryService(workflows, executions, new FakeApprovalRepository());

        var summary = await service.GetSummaryAsync();

        Assert.Equal(2, summary.ExecutionsToday);
        Assert.Equal(0, summary.FailuresToday);
        Assert.Equal(100d, summary.SuccessRatePercent);
        Assert.True(summary.AverageExecutionDurationMs >= 0);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsOnlyPendingApprovals()
    {
        var approvals = new FakeApprovalRepository();
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());
        var approvalService = new ApprovalService(approvals, engine);
        await approvalService.CreateAsync(ApprovalType.Leave, "Leave A", "", ["Manager"], "user-1", "org-1", "branch-1");
        var resolved = await approvalService.CreateAsync(ApprovalType.Leave, "Leave B", "", ["Manager"], "user-2", "org-1", "branch-1");
        await approvalService.DecideAsync(resolved.Id, approve: true, "manager-1", null);
        var service = new AutomationDashboardQueryService(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository(), approvals);

        var summary = await service.GetSummaryAsync();

        Assert.Equal(1, summary.PendingApprovals);
    }
}
