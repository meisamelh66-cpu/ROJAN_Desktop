namespace Rojan.Desktop.Application.Automation;

/// <summary>
/// The Trigger Engine (Requirement 32.3) - the one entry point every
/// business event (Appointment Created, Customer Registered, Login, ...)
/// is raised through, dispatching to every enabled
/// <see cref="WorkflowStatus.Published"/> workflow subscribed to that
/// trigger. No existing module calls this yet in this phase (Requirement
/// 32's own "do not modify existing modules unless integration is
/// required") - it is exercised today via a manual "Simulate Trigger"
/// action in the Automation module and by tests; wiring real callers
/// (Bookings creating an appointment, Identity's login flow, ...) is a
/// documented future integration, one call each, once this phase is
/// reviewed.
/// </summary>
public interface ITriggerEngine
{
    /// <summary>Starts a run of every matching workflow, returning each started execution.</summary>
    public Task<IReadOnlyList<WorkflowExecutionDto>> RaiseAsync(
        TriggerType trigger,
        IReadOnlyDictionary<string, string> facts,
        string organizationId,
        string branchId,
        string triggeredByUserId,
        CancellationToken cancellationToken = default);
}
