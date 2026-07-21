namespace Rojan.Desktop.Domain.Automation;

/// <summary>Pure state-machine logic advancing an <see cref="ApprovalRequest"/> through its <see cref="ApprovalStep"/>s - a single Reject at any step rejects the whole request; approving the last step approves the whole request; approving a non-final step advances <see cref="ApprovalRequest.CurrentStepIndex"/> to the next one.</summary>
public static class ApprovalRules
{
    /// <summary>Records a decision on the request's current step, returning the updated request. Throws <see cref="InvalidOperationException"/> if the request is already in a terminal state (<see cref="ApprovalStatus.Approved"/>/<see cref="ApprovalStatus.Rejected"/>/<see cref="ApprovalStatus.Cancelled"/>).</summary>
    public static ApprovalRequest Decide(ApprovalRequest request, bool approve, string userId, string? comment, DateTimeOffset now)
    {
        if (request.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Approval request '{request.Id}' is already {request.Status} and cannot be decided again.");
        }

        var steps = request.Steps.ToList();
        var currentStep = steps[request.CurrentStepIndex];
        steps[request.CurrentStepIndex] = currentStep with
        {
            Status = approve ? ApprovalStepStatus.Approved : ApprovalStepStatus.Rejected,
            DecidedByUserId = userId,
            DecidedAt = now,
            Comment = comment,
        };

        if (!approve)
        {
            return request with { Steps = steps, Status = ApprovalStatus.Rejected };
        }

        var isFinalStep = request.CurrentStepIndex == steps.Count - 1;
        return isFinalStep
            ? request with { Steps = steps, Status = ApprovalStatus.Approved }
            : request with { Steps = steps, CurrentStepIndex = request.CurrentStepIndex + 1 };
    }

    public static ApprovalStep? CurrentStep(ApprovalRequest request) =>
        request.Status == ApprovalStatus.Pending && request.CurrentStepIndex < request.Steps.Count
            ? request.Steps[request.CurrentStepIndex]
            : null;

    public static bool IsTerminal(ApprovalStatus status) =>
        status is ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Cancelled;
}
