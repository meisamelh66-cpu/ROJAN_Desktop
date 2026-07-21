namespace Rojan.Desktop.Domain.Automation;

/// <summary>
/// One node in a <see cref="WorkflowDefinition"/>'s step graph, as returned
/// by <see cref="IWorkflowRepository"/>. <see cref="Config"/> is a flat
/// string-keyed bag (e.g. Delay's "seconds", Notification's "messageKey",
/// Condition's "field"/"operator"/"value") - deliberately untyped so a
/// future visual designer (Requirement 32.1) can persist/edit steps
/// without Domain needing a bespoke settings type per
/// <see cref="WorkflowStepType"/>. <see cref="Branches"/> is populated only
/// for <see cref="WorkflowStepType.Decision"/> - a branch-name (e.g.
/// "true"/"false") to next-step-id map; every other step type uses the
/// single <see cref="NextStepId"/> instead.
/// </summary>
public sealed record WorkflowStep(
    string Id,
    WorkflowStepType Type,
    string Name,
    IReadOnlyDictionary<string, string> Config,
    string? NextStepId,
    IReadOnlyDictionary<string, string>? Branches);
