namespace Rojan.Desktop.Application.Automation;

/// <summary>
/// Runs a <c>WorkflowDefinition</c>'s step graph end to end (or until it
/// pauses on an <see cref="WorkflowStepType.Approval"/> step) - the one
/// place Requirement 32.1 (step types)/32.8 (Monitoring)/32.10 (Audit)/
/// 32.11 (Error Recovery) actually come together. Every run (and every
/// resume) persists a <see cref="WorkflowExecutionDto"/> with full
/// per-step logs, regardless of outcome.
/// </summary>
public interface IWorkflowExecutionEngine
{
    /// <summary>Starts a fresh run of <paramref name="workflowId"/> from its Start step. <paramref name="facts"/> is the trigger/business context every <see cref="WorkflowStepType.Decision"/>/<see cref="WorkflowStepType.Condition"/> step evaluates against.</summary>
    public Task<WorkflowExecutionDto> ExecuteAsync(
        string workflowId,
        TriggerType? trigger,
        string triggeredByUserId,
        IReadOnlyDictionary<string, string> facts,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default);

    /// <summary>Every recorded execution, most recent first - Requirement 32.8's monitoring/history source.</summary>
    public Task<IReadOnlyList<WorkflowExecutionDto>> GetHistoryAsync(CancellationToken cancellationToken = default);

    public Task<WorkflowExecutionDto?> GetByIdAsync(string executionId, CancellationToken cancellationToken = default);

    /// <summary>Continues an execution paused in <see cref="WorkflowExecutionStatus.Waiting"/> once its linked <c>ApprovalRequest</c> is decided - called by <c>ApprovalService.DecideAsync</c>, not directly by Presentation. Rejecting fails the execution; approving resumes the graph from the step after the Approval step.</summary>
    public Task<WorkflowExecutionDto> ResumeApprovalAsync(string executionId, bool approved, CancellationToken cancellationToken = default);
}
