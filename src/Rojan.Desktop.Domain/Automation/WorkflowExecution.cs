namespace Rojan.Desktop.Domain.Automation;

/// <summary>One step's run within a single <see cref="WorkflowExecution"/> - Requirement 32.10 (Audit)'s per-step detail.</summary>
public sealed record WorkflowStepExecutionLog(
    string StepId,
    string StepName,
    WorkflowStepType StepType,
    StepExecutionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int AttemptCount,
    string? ErrorMessage);

/// <summary>
/// One run of a <see cref="WorkflowDefinition"/>, as returned by
/// <see cref="IWorkflowExecutionRepository"/> - Requirement 32.8 (Workflow
/// Monitoring) and 32.10 (Audit)'s source of truth: every execution
/// records who/what triggered it, when, how long it took, and (on
/// failure) why.
/// </summary>
public sealed record WorkflowExecution(
    string Id,
    string WorkflowId,
    int WorkflowVersion,
    string WorkflowName,
    WorkflowExecutionStatus Status,
    TriggerType? TriggeredByTrigger,
    string TriggeredByUserId,
    IReadOnlyList<WorkflowStepExecutionLog> StepLogs,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMs,
    string? ErrorMessage,
    string OrganizationId,
    string BranchId);
