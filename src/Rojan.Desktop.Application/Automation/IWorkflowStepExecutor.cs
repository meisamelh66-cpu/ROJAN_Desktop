namespace Rojan.Desktop.Application.Automation;

/// <summary>Everything a <see cref="IWorkflowStepExecutor"/> needs to run one step - the running execution's id (so an <see cref="WorkflowStepType.Approval"/> step can link the <c>ApprovalRequest</c> it raises back to this execution), the trigger/business fact bag, and the tenant/user scope.</summary>
public sealed record AutomationExecutionContext(
    string ExecutionId,
    IReadOnlyDictionary<string, string> Facts,
    string OrganizationId,
    string BranchId,
    string TriggeredByUserId);

/// <summary>
/// One step's outcome, as reported back to <c>WorkflowExecutionEngine</c>'s
/// run loop. <see cref="BranchResult"/> is read only for
/// <see cref="WorkflowStepType.Decision"/> steps (resolved against
/// <c>Domain.Automation.WorkflowStep.Branches</c>).
/// <see cref="IsWaiting"/> pauses the whole execution
/// (<see cref="WorkflowExecutionStatus.Waiting"/>) - only
/// <see cref="WorkflowStepType.Approval"/> sets it. <see cref="StopWorkflow"/>
/// ends the run successfully without following any further step - only
/// <see cref="WorkflowStepType.Condition"/> sets it, when its condition
/// evaluates false.
/// </summary>
public sealed record StepExecutionResult(bool IsSuccess, string? BranchResult, bool IsWaiting, bool StopWorkflow, string? ErrorMessage)
{
    public static StepExecutionResult Success(string? branchResult = null) => new(true, branchResult, false, false, null);

    public static StepExecutionResult Waiting() => new(true, null, true, false, null);

    public static StepExecutionResult Stop() => new(true, null, false, true, null);

    public static StepExecutionResult Failure(string error) => new(false, null, false, false, error);
}

/// <summary>Executes exactly one <see cref="WorkflowStepType"/> - one implementation per type, dispatched by <c>WorkflowExecutionEngine</c> via a <see cref="StepType"/>-keyed lookup built from every registered instance (<c>IEnumerable&lt;IWorkflowStepExecutor&gt;</c>).</summary>
public interface IWorkflowStepExecutor
{
    public WorkflowStepType StepType { get; }

    public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default);
}
