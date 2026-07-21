namespace Rojan.Desktop.Application.Automation;

/// <summary>Application's own mirror of <c>Domain.Automation.RetryPolicy</c>.</summary>
public sealed record RetryPolicyDto(int MaxRetries, int RetryDelaySeconds, int TimeoutSeconds)
{
    public static RetryPolicyDto None { get; } = new(0, 0, 30);
}

/// <summary>Application's own mirror of <c>Domain.Automation.WorkflowStep</c>.</summary>
public sealed record WorkflowStepDto(
    string Id,
    WorkflowStepType Type,
    string Name,
    IReadOnlyDictionary<string, string> Config,
    string? NextStepId,
    IReadOnlyDictionary<string, string>? Branches);

/// <summary>Application's own mirror of <c>Domain.Automation.WorkflowDefinition</c>.</summary>
public sealed record WorkflowDefinitionDto(
    string Id,
    string ParentWorkflowId,
    string Name,
    string Description,
    IReadOnlyList<WorkflowStepDto> Steps,
    IReadOnlyList<TriggerType> TriggerTypes,
    WorkflowStatus Status,
    int Version,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedByUserId,
    string OrganizationId,
    string BranchId);

/// <summary>Application's own mirror of <c>Domain.Automation.WorkflowStepExecutionLog</c>.</summary>
public sealed record WorkflowStepExecutionLogDto(
    string StepId,
    string StepName,
    WorkflowStepType StepType,
    StepExecutionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int AttemptCount,
    string? ErrorMessage);

/// <summary>Application's own mirror of <c>Domain.Automation.WorkflowExecution</c>.</summary>
public sealed record WorkflowExecutionDto(
    string Id,
    string WorkflowId,
    int WorkflowVersion,
    string WorkflowName,
    WorkflowExecutionStatus Status,
    TriggerType? TriggeredByTrigger,
    string TriggeredByUserId,
    IReadOnlyList<WorkflowStepExecutionLogDto> StepLogs,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMs,
    string? ErrorMessage,
    string OrganizationId,
    string BranchId);
