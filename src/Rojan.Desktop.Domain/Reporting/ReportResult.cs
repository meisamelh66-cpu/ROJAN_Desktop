namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// The output of running one <see cref="ReportDefinition"/> with a given
/// set of <see cref="ReportFilter"/>s - never persisted itself (a
/// <see cref="ReportSnapshot"/> records that a run happened, not the full
/// row data, keeping history storage small since results are cheap to
/// regenerate from the same repositories).
/// </summary>
public sealed record ReportResult(
    string ReportDefinitionId,
    string ReportName,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<ReportRow> Rows,
    IReadOnlyList<ReportFilter> AppliedFilters,
    IReadOnlyDictionary<string, string> Summary);
