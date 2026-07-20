namespace Rojan.Desktop.Domain.AI;

/// <summary>The AI Dashboard's single headline number (0-100) - a weighted composite of <see cref="Components"/>, computed by <see cref="BusinessHealthCalculator"/>.</summary>
public sealed record BusinessHealthScore(
    decimal OverallScore,
    IReadOnlyList<BusinessHealthComponent> Components,
    string Summary,
    DateTimeOffset ComputedAt);
