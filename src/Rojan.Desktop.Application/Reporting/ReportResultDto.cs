namespace Rojan.Desktop.Application.Reporting;

public sealed record ReportResultDto(
    string ReportDefinitionId,
    string ReportName,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReportColumnDto> Columns,
    IReadOnlyList<ReportRowDto> Rows,
    IReadOnlyList<ReportFilterDto> AppliedFilters,
    IReadOnlyDictionary<string, string> Summary);
