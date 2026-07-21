using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>Default <see cref="IApprovalService"/>. Wraps <c>Domain.Automation.ApprovalRules.Decide</c>'s pure state machine with persistence and, when a decision resolves a workflow-originated request, resumes/fails the paused <c>WorkflowExecution</c> via <see cref="IWorkflowExecutionEngine"/> - the one place these two subsystems connect.</summary>
public sealed class ApprovalService : IApprovalService
{
    private readonly DomainAutomation.IApprovalRepository _repository;
    private readonly IWorkflowExecutionEngine _executionEngine;

    public ApprovalService(DomainAutomation.IApprovalRepository repository, IWorkflowExecutionEngine executionEngine)
    {
        _repository = repository;
        _executionEngine = executionEngine;
    }

    public async Task<IReadOnlyList<ApprovalRequestDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return requests.OrderByDescending(request => request.RequestedAt).Select(AutomationMapping.Map).ToList();
    }

    public async Task<ApprovalRequestDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return request is null ? null : AutomationMapping.Map(request);
    }

    public async Task<IReadOnlyList<ApprovalRequestDto>> GetPendingForRoleAsync(string approverRole, CancellationToken cancellationToken = default)
    {
        var requests = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return requests
            .Where(request => request.Status == DomainAutomation.ApprovalStatus.Pending
                && DomainAutomation.ApprovalRules.CurrentStep(request) is { } currentStep
                && string.Equals(currentStep.ApproverRole, approverRole, StringComparison.OrdinalIgnoreCase))
            .OrderBy(request => request.RequestedAt)
            .Select(AutomationMapping.Map)
            .ToList();
    }

    public async Task<ApprovalRequestDto> CreateAsync(
        ApprovalType type,
        string title,
        string description,
        IReadOnlyList<string> approverRoles,
        string requestedByUserId,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        if (approverRoles.Count == 0)
        {
            throw new ArgumentException("An approval request needs at least one approver role.", nameof(approverRoles));
        }

        var steps = approverRoles
            .Select((role, index) => new DomainAutomation.ApprovalStep(index, role, DomainAutomation.ApprovalStepStatus.Pending, null, null, null))
            .ToList();

        var request = new DomainAutomation.ApprovalRequest(
            Guid.NewGuid().ToString("N"), AutomationMapping.MapToDomain(type), title, description, requestedByUserId,
            DateTimeOffset.UtcNow, steps, DomainAutomation.ApprovalStatus.Pending, CurrentStepIndex: 0,
            WorkflowExecutionId: null, organizationId, branchId);

        await _repository.SaveAsync(request, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(request);
    }

    public async Task<ApprovalRequestDto> DecideAsync(string requestId, bool approve, string userId, string? comment, CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Approval request '{requestId}' was not found.");

        var decided = DomainAutomation.ApprovalRules.Decide(request, approve, userId, comment, DateTimeOffset.UtcNow);
        await _repository.SaveAsync(decided, cancellationToken).ConfigureAwait(false);

        if (DomainAutomation.ApprovalRules.IsTerminal(decided.Status) && decided.WorkflowExecutionId is { } executionId)
        {
            await _executionEngine.ResumeApprovalAsync(executionId, decided.Status == DomainAutomation.ApprovalStatus.Approved, cancellationToken).ConfigureAwait(false);
        }

        return AutomationMapping.Map(decided);
    }
}
