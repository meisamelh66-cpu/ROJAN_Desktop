namespace Rojan.Desktop.Domain.Reporting;

/// <summary>One row of a <see cref="ReportResult"/> - values are pre-formatted display strings, keyed by the owning <see cref="ReportDefinition"/>'s <see cref="ReportColumn.Key"/>.</summary>
public sealed record ReportRow(IReadOnlyDictionary<string, string> Values);
