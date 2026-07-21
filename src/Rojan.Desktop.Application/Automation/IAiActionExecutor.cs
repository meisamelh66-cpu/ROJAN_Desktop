namespace Rojan.Desktop.Application.Automation;

/// <summary>One <see cref="WorkflowStepType.AiAction"/> step's configured request.</summary>
public sealed record AiActionRequest(string ActionKey, IReadOnlyDictionary<string, string> Parameters);

/// <summary>The outcome of an <see cref="IAiActionExecutor"/> call.</summary>
public sealed record AiActionResult(bool IsSuccess, string? Output, string? ErrorMessage);

/// <summary>
/// Integration point for ROJAN AI-driven workflow steps - Requirement
/// 32.7 ("AI Automation Ready... Prepare integration points for ROJAN
/// AI. No external AI calls yet. Only architecture and contracts.").
/// <see cref="NoOpAiActionExecutor"/> is the only implementation
/// registered in this phase; wiring this to the real ROJAN AI Center
/// (Phase 21) is a documented future integration, not built here.
/// </summary>
public interface IAiActionExecutor
{
    public Task<AiActionResult> ExecuteAsync(AiActionRequest request, CancellationToken cancellationToken = default);
}

/// <summary>The only <see cref="IAiActionExecutor"/> in this phase - always succeeds without making any external call, per Requirement 32.7's explicit "no external AI calls yet" boundary.</summary>
public sealed class NoOpAiActionExecutor : IAiActionExecutor
{
    public Task<AiActionResult> ExecuteAsync(AiActionRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiActionResult(IsSuccess: true, Output: "AI action recorded - architecture-only in this phase, no external AI call made.", ErrorMessage: null));
}
