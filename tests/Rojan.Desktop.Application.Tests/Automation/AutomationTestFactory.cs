using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Assembles a real <see cref="WorkflowExecutionEngine"/> with every real step executor over fake repositories/stub services - the same "exercise real Application logic over a fake repository" shape this codebase's other test factories (e.g. Shell.Tests.Navigation.TestSearchServices) already establish.</summary>
internal static class AutomationTestFactory
{
    public static WorkflowExecutionEngine CreateExecutionEngine(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository,
        IApprovalRepository? approvalRepository = null,
        StubNotificationService? notificationService = null,
        StubEmailNotificationService? emailService = null)
    {
        var executors = new List<IWorkflowStepExecutor>
        {
            new StartStepExecutor(),
            new EndStepExecutor(),
            new DecisionStepExecutor(),
            new ConditionStepExecutor(),
            new DelayStepExecutor(),
            new ApprovalStepExecutor(approvalRepository ?? new FakeApprovalRepository()),
            new NotificationStepExecutor(notificationService ?? new StubNotificationService()),
            new EmailStepExecutor(emailService ?? new StubEmailNotificationService()),
            new AiActionStepExecutor(new NoOpAiActionExecutor()),
            new DatabaseActionStepExecutor(),
            new ApiActionStepExecutor(),
        };

        return new WorkflowExecutionEngine(workflowRepository, executionRepository, executors);
    }

    public static WorkflowStepDto Step(string id, Application.Automation.WorkflowStepType type, string? nextId = null, IReadOnlyDictionary<string, string>? config = null, IReadOnlyDictionary<string, string>? branches = null) =>
        new(id, type, id, config ?? new Dictionary<string, string>(), nextId, branches);
}
