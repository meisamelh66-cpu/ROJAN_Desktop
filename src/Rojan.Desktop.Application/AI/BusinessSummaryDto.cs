namespace Rojan.Desktop.Application.AI;

public sealed record BusinessSummaryDto(
    string Title,
    string NarrativeText,
    IReadOnlyList<string> Highlights,
    DateTimeOffset GeneratedAt);
