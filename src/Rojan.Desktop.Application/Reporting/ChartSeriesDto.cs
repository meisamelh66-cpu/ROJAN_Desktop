namespace Rojan.Desktop.Application.Reporting;

public sealed record ChartSeriesDto(string Label, IReadOnlyList<decimal> Values);
