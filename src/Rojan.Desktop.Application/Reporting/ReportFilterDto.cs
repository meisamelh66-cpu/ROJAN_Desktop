namespace Rojan.Desktop.Application.Reporting;

/// <summary>One filter value to apply when running a report. <see cref="Value"/> encoding matches <see cref="Rojan.Desktop.Domain.Reporting.ReportFilter"/>'s - see <see cref="ReportFilterSet"/> for parsing.</summary>
public sealed record ReportFilterDto(FilterType FilterType, string Value, string DisplayLabel);
