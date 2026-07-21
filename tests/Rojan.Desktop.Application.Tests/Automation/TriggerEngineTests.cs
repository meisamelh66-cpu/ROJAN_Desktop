using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Exercises <see cref="TriggerEngine"/> - Requirement 32.3's fan-out from a raised trigger to every subscribed, Published, enabled workflow.</summary>
public sealed class TriggerEngineTests
{
    private static IReadOnlyList<WorkflowStepDto> ValidSteps(out string startId, out string endId)
    {
        startId = Guid.NewGuid().ToString("N");
        endId = Guid.NewGuid().ToString("N");
        return
        [
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        ];
    }

    [Fact]
    public async Task RaiseAsync_OnlyExecutesPublishedEnabledWorkflowsSubscribedToTheTrigger()
    {
        var workflows = new FakeWorkflowRepository();
        var executions = new FakeWorkflowExecutionRepository();
        var service = new WorkflowService(workflows);
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);
        var triggerEngine = new TriggerEngine(workflows, engine);

        var subscribed = await service.CreateDraftAsync("Subscribed", "", ValidSteps(out _, out _), [TriggerType.AppointmentCreated], "user-1", "org-1", "branch-1");
        await service.PublishAsync(subscribed.Id);

        var notSubscribed = await service.CreateDraftAsync("Not Subscribed", "", ValidSteps(out _, out _), [TriggerType.CustomerRegistered], "user-1", "org-1", "branch-1");
        await service.PublishAsync(notSubscribed.Id);

        var stillDraft = await service.CreateDraftAsync("Still Draft", "", ValidSteps(out _, out _), [TriggerType.AppointmentCreated], "user-1", "org-1", "branch-1");

        var results = await triggerEngine.RaiseAsync(TriggerType.AppointmentCreated, new Dictionary<string, string>(), "org-1", "branch-1", "user-1");

        Assert.Single(results);
        Assert.Equal(subscribed.Id, results[0].WorkflowId);
    }

    [Fact]
    public async Task RaiseAsync_NoSubscribers_ReturnsEmpty()
    {
        var workflows = new FakeWorkflowRepository();
        var executions = new FakeWorkflowExecutionRepository();
        var engine = AutomationTestFactory.CreateExecutionEngine(workflows, executions);
        var triggerEngine = new TriggerEngine(workflows, engine);

        var results = await triggerEngine.RaiseAsync(TriggerType.LicenseExpired, new Dictionary<string, string>(), "org-1", "branch-1", "user-1");

        Assert.Empty(results);
    }
}
