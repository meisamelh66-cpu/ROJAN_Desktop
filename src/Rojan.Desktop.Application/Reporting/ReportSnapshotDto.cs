namespace Rojan.Desktop.Application.Reporting;

public sealed record ReportSnapshotDto(
    string Id,
    string ReportDefinitionId,
    string ReportName,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReportFilterDto> AppliedFilters,
    int RowCount,
    bool IsSaved);
