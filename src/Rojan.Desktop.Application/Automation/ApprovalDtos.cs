namespace Rojan.Desktop.Application.Automation;

/// <summary>Application's own mirror of <c>Domain.Automation.ApprovalType</c>.</summary>
public enum ApprovalType
{
    Leave,
    Expense,
    Inventory,
    Branch,
}

/// <summary>Application's own mirror of <c>Domain.Automation.ApprovalStatus</c>.</summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

/// <summary>Application's own mirror of <c>Domain.Automation.ApprovalStepStatus</c>.</summary>
public enum ApprovalStepStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>Application's own mirror of <c>Domain.Automation.ApprovalStep</c>.</summary>
public sealed record ApprovalStepDto(
    int StepIndex,
    string ApproverRole,
    ApprovalStepStatus Status,
    string? DecidedByUserId,
    DateTimeOffset? DecidedAt,
    string? Comment);

/// <summary>Application's own mirror of <c>Domain.Automation.ApprovalRequest</c>.</summary>
public sealed record ApprovalRequestDto(
    string Id,
    ApprovalType Type,
    string Title,
    string Description,
    string RequestedByUserId,
    DateTimeOffset RequestedAt,
    IReadOnlyList<ApprovalStepDto> Steps,
    ApprovalStatus Status,
    int CurrentStepIndex,
    string? WorkflowExecutionId,
    string OrganizationId,
    string BranchId);
