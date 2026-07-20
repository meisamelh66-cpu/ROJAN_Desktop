namespace Rojan.Desktop.Domain.AI;

/// <summary>
/// A reusable prompt shape the Prompt Templates UI lists and
/// <c>Application.AI.PromptBuilder</c> fills in - <see cref="Body"/> uses
/// <c>{placeholder}</c> tokens (e.g. <c>"{period}"</c>) substituted from
/// whatever <see cref="Rojan.Desktop.Domain.Reporting"/> or other context
/// the caller supplies.
/// </summary>
public sealed record PromptTemplate(
    string Id,
    string Name,
    InsightCategory Category,
    string Body,
    bool IsSystemDefined);
