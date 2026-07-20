namespace Rojan.Desktop.Domain.AI;

/// <summary>One weighted input into <see cref="BusinessHealthScore"/> - see <see cref="BusinessHealthCalculator"/> for how these combine.</summary>
public sealed record BusinessHealthComponent(InsightCategory Category, string Label, decimal Score, decimal Weight);
