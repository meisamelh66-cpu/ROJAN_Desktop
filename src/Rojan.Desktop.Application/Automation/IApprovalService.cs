namespace Rojan.Desktop.Application.Automation;

/// <summary>Multi-step Approval Workflow (Requirement 32.5) - Leave/Expense/Inventory/Branch, plus whatever a workflow's <c>Approval</c> step raises automatically.</summary>
public interface IApprovalService
{
    public Task<IReadOnlyList<ApprovalRequestDto>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<ApprovalRequestDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Every <see cref="ApprovalStatus.Pending"/> request whose current step's approver role matches <paramref name="approverRole"/> - one user's "needs my decision" queue.</summary>
    public Task<IReadOnlyList<ApprovalRequestDto>> GetPendingForRoleAsync(string approverRole, CancellationToken cancellationToken = default);

    /// <summary>Raises a new, standalone (not workflow-driven) multi-step approval - one <see cref="ApprovalStepDto"/> per entry in <paramref name="approverRoles"/>, in order.</summary>
    public Task<ApprovalRequestDto> CreateAsync(
        ApprovalType type,
        string title,
        string description,
        IReadOnlyList<string> approverRoles,
        string requestedByUserId,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default);

    /// <summary>Records a decision on the request's current step. If the request reaches a terminal state and it was raised by a workflow's Approval step (<see cref="ApprovalRequestDto.WorkflowExecutionId"/> set), automatically resumes (or fails) that paused execution.</summary>
    public Task<ApprovalRequestDto> DecideAsync(string requestId, bool approve, string userId, string? comment, CancellationToken cancellationToken = default);
}
