namespace Rojan.Desktop.Domain.Reporting;

/// <summary>One data series of a <see cref="ChartDefinition"/> - <see cref="Values"/> is index-aligned with the owning chart's <c>Categories</c>.</summary>
public sealed record ChartSeries(string Label, IReadOnlyList<decimal> Values);
