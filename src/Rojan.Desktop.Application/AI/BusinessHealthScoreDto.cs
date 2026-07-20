namespace Rojan.Desktop.Application.AI;

public sealed record BusinessHealthScoreDto(
    decimal OverallScore,
    IReadOnlyList<BusinessHealthComponentDto> Components,
    string Summary,
    DateTimeOffset ComputedAt);
