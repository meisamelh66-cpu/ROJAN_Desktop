using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Exercises <see cref="WorkflowExecutionEngine"/> - the step-graph run loop, Decision branching, Condition gating, Approval pause/resume, and Requirement 32.11's retry-on-failure.</summary>
public sealed class WorkflowExecutionEngineTests
{
    /// <summary>Fails its first <see cref="FailuresBeforeSuccess"/> calls, then succeeds - lets a test exercise <see cref="WorkflowExecutionEngine"/>'s retry loop deterministically. Registered as the DatabaseAction executor so it can be dropped into a normal workflow step without needing a new <see cref="WorkflowStepType"/>.</summary>
    private sealed class FlakyStepExecutor : IWorkflowStepExecutor
    {
        private int _callCount;

        public int FailuresBeforeSuccess { get; set; }

        public WorkflowStepType StepType => WorkflowStepType.DatabaseAction;

        public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default)
        {
            _callCount++;
            return Task.FromResult(_callCount <= FailuresBeforeSuccess ? StepExecutionResult.Failure("simulated failure") : StepExecutionResult.Success());
        }
    }

    private static async Task<(FakeWorkflowRepository Workflows, FakeWorkflowExecutionRepository Executions, string WorkflowId)> SeedWorkflowAsync(IReadOnlyList<WorkflowStepDto> steps)
    {
        var workflows = new FakeWorkflowRepository();
        var executions = new FakeWorkflowExecutionRepository();
        var service = new WorkflowService(workflows);
        var draft = await service.CreateDraftAsync("Test Workflow", "", steps, [], "user-1", "org-1", "branch-1");
        await service.PublishAsync(draft.Id);
        return (workflows, executions, draft.Id);
    }

    [Fact]
    public async Task ExecuteAsync_SimpleStartEnd_CompletesSuccessfully()
    {
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);

        var execution = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        Assert.Equal(WorkflowExecutionStatus.Completed, execution.Status);
        Assert.NotNull(execution.DurationMs);
        Assert.Equal(2, execution.StepLogs.Count);
    }

    [Fact]
    public async Task ExecuteAsync_DecisionStep_FollowsTheMatchingBranch()
    {
        var startId = Guid.NewGuid().ToString("N");
        var decisionId = Guid.NewGuid().ToString("N");
        var trueEndId = Guid.NewGuid().ToString("N");
        var falseEndId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, decisionId),
            AutomationTestFactory.Step(decisionId, WorkflowStepType.Decision, null,
                new Dictionary<string, string> { ["field"] = "IsVip", ["operator"] = "Equals", ["value"] = "true" },
                new Dictionary<string, string> { ["true"] = trueEndId, ["false"] = falseEndId }),
            AutomationTestFactory.Step(trueEndId, WorkflowStepType.End),
            AutomationTestFactory.Step(falseEndId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);

        var execution = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string> { ["IsVip"] = "true" }, "org-1", "branch-1");

        Assert.Equal(WorkflowExecutionStatus.Completed, execution.Status);
        Assert.Equal(trueEndId, execution.StepLogs[^1].StepId);
    }

    [Fact]
    public async Task ExecuteAsync_ConditionStepFalse_StopsWithoutFollowingItsNextStep()
    {
        var startId = Guid.NewGuid().ToString("N");
        var conditionId = Guid.NewGuid().ToString("N");
        var unreachableEndId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, conditionId),
            AutomationTestFactory.Step(conditionId, WorkflowStepType.Condition, unreachableEndId,
                new Dictionary<string, string> { ["field"] = "Stock", ["operator"] = "LessThan", ["value"] = "5" }),
            AutomationTestFactory.Step(unreachableEndId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);

        var execution = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string> { ["Stock"] = "20" }, "org-1", "branch-1");

        Assert.Equal(WorkflowExecutionStatus.Completed, execution.Status);
        Assert.Equal(2, execution.StepLogs.Count);
        Assert.Equal(conditionId, execution.StepLogs[^1].StepId);
    }

    [Fact]
    public async Task ExecuteAsync_ApprovalStep_PausesExecutionAsWaiting()
    {
        var startId = Guid.NewGuid().ToString("N");
        var approvalId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, approvalId),
            AutomationTestFactory.Step(approvalId, WorkflowStepType.Approval, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var approvals = new FakeApprovalRepository();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions, approvals);

        var execution = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        Assert.Equal(WorkflowExecutionStatus.Waiting, execution.Status);
        var approvalRequests = await approvals.GetAllAsync();
        Assert.Single(approvalRequests);
        Assert.Equal(execution.Id, approvalRequests[0].WorkflowExecutionId);
    }

    [Fact]
    public async Task ResumeApprovalAsync_Approved_ContinuesToEndAndCompletes()
    {
        var startId = Guid.NewGuid().ToString("N");
        var approvalId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, approvalId),
            AutomationTestFactory.Step(approvalId, WorkflowStepType.Approval, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions, new FakeApprovalRepository());
        var paused = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        var resumed = await engine.ResumeApprovalAsync(paused.Id, approved: true);

        Assert.Equal(WorkflowExecutionStatus.Completed, resumed.Status);
        Assert.Equal(3, resumed.StepLogs.Count);
    }

    [Fact]
    public async Task ResumeApprovalAsync_Rejected_FailsTheExecution()
    {
        var startId = Guid.NewGuid().ToString("N");
        var approvalId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, approvalId),
            AutomationTestFactory.Step(approvalId, WorkflowStepType.Approval, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions, new FakeApprovalRepository());
        var paused = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        var resumed = await engine.ResumeApprovalAsync(paused.Id, approved: false);

        Assert.Equal(WorkflowExecutionStatus.Failed, resumed.Status);
        Assert.NotNull(resumed.ErrorMessage);
    }

    [Fact]
    public async Task ResumeApprovalAsync_NotWaiting_Throws()
    {
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);
        var completed = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.ResumeApprovalAsync(completed.Id, true));
    }

    [Fact]
    public async Task ExecuteAsync_StepFailsFewerTimesThanMaxRetries_EventuallySucceeds()
    {
        var startId = Guid.NewGuid().ToString("N");
        var flakyId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, flakyId),
            AutomationTestFactory.Step(flakyId, WorkflowStepType.DatabaseAction, endId, new Dictionary<string, string> { ["maxRetries"] = "3", ["retryDelaySeconds"] = "0" }),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var flaky = new FlakyStepExecutor { FailuresBeforeSuccess = 2 };
        var engine = new WorkflowExecutionEngine(workflows, executions, [new StartStepExecutor(), new EndStepExecutor(), flaky]);

        var execution = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        Assert.Equal(WorkflowExecutionStatus.Completed, execution.Status);
        Assert.Equal(3, execution.StepLogs.Single(log => log.StepId == flakyId).AttemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_StepFailsMoreTimesThanMaxRetries_FailsTheWholeExecution()
    {
        var startId = Guid.NewGuid().ToString("N");
        var flakyId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, flakyId),
            AutomationTestFactory.Step(flakyId, WorkflowStepType.DatabaseAction, endId, new Dictionary<string, string> { ["maxRetries"] = "1", ["retryDelaySeconds"] = "0" }),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var flaky = new FlakyStepExecutor { FailuresBeforeSuccess = 5 };
        var engine = new WorkflowExecutionEngine(workflows, executions, [new StartStepExecutor(), new EndStepExecutor(), flaky]);

        var execution = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        Assert.Equal(WorkflowExecutionStatus.Failed, execution.Status);
        Assert.Equal(2, execution.StepLogs.Single(log => log.StepId == flakyId).AttemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownWorkflowId_Throws()
    {
        var engine = AutomationTestFactory.CreateExecutionEngine(new FakeWorkflowRepository(), new FakeWorkflowExecutionRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.ExecuteAsync("missing", null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1"));
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsMostRecentFirst()
    {
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        var steps = new[]
        {
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        };
        var (workflows, executions, workflowId) = await SeedWorkflowAsync(steps);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);
        await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");
        await Task.Delay(10);
        var second = await engine.ExecuteAsync(workflowId, null, "user-1", new Dictionary<string, string>(), "org-1", "branch-1");

        var history = await engine.GetHistoryAsync();

        Assert.Equal(second.Id, history[0].Id);
    }
}
