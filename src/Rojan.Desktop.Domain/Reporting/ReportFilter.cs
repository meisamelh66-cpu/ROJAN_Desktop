namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// One applied filter value. <see cref="Value"/> is always a plain string
/// so the same shape covers every <see cref="FilterType"/> - a
/// <see cref="FilterType.DateRange"/> filter encodes both bounds as
/// <c>"yyyy-MM-dd|yyyy-MM-dd"</c> (parsed by
/// <c>Application.Reporting.ReportFilterSet</c>), every other filter type
/// carries a single id/status/category value.
/// </summary>
public sealed record ReportFilter(FilterType FilterType, string Value, string DisplayLabel);
