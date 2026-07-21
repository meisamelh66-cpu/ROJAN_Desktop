namespace Rojan.Desktop.Domain.Automation;

/// <summary>What an <see cref="ApprovalRequest"/> is for - Requirement 32.5's own examples.</summary>
public enum ApprovalType
{
    Leave,
    Expense,
    Inventory,
    Branch,
}

/// <summary>An <see cref="ApprovalRequest"/>'s overall state - derived from its <see cref="ApprovalStep"/>s by <see cref="ApprovalRules"/>.</summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

/// <summary>One <see cref="ApprovalStep"/>'s own decision state.</summary>
public enum ApprovalStepStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>
/// One approver in a multi-step <see cref="ApprovalRequest"/>.
/// <see cref="ApproverRole"/> is a free-form role name (e.g.
/// "BranchManager"), not <c>Organizations.WorkspaceRole</c> directly - the
/// same "Domain stays a dumb data shape, no cross-slice dependency"
/// reasoning <c>Bookings.Booking.SpecialistId</c> already establishes;
/// resolving a role name to an actual approver/session lives in
/// Application/Presentation.
/// </summary>
public sealed record ApprovalStep(
    int StepIndex,
    string ApproverRole,
    ApprovalStepStatus Status,
    string? DecidedByUserId,
    DateTimeOffset? DecidedAt,
    string? Comment);

/// <summary>
/// A multi-step approval request (Requirement 32.5) - Leave/Expense/
/// Inventory/Branch, as returned by <see cref="IApprovalRepository"/>.
/// <see cref="CurrentStepIndex"/> is the <see cref="ApprovalStep"/> whose
/// decision is still pending; <see cref="ApprovalRules.Decide"/> is the
/// one place that advances it. <see cref="WorkflowExecutionId"/> is set
/// only when this request was raised by a workflow's
/// <see cref="WorkflowStepType.Approval"/> step (as opposed to a
/// standalone approval raised directly through the Approvals module) -
/// once decided, it is what lets
/// <c>Application.Automation.IWorkflowExecutionEngine.ResumeApprovalAsync</c>
/// know which paused (<see cref="WorkflowExecutionStatus.Waiting"/>)
/// execution to continue.
/// </summary>
public sealed record ApprovalRequest(
    string Id,
    ApprovalType Type,
    string Title,
    string Description,
    string RequestedByUserId,
    DateTimeOffset RequestedAt,
    IReadOnlyList<ApprovalStep> Steps,
    ApprovalStatus Status,
    int CurrentStepIndex,
    string? WorkflowExecutionId,
    string OrganizationId,
    string BranchId);
