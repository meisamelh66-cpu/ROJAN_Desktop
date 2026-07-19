namespace Rojan.Desktop.Application.Reporting;

public sealed record ReportDefinitionDto(
    string Id,
    string Name,
    string Description,
    ReportType ReportType,
    ReportCategory Category,
    bool IsSystemDefined,
    IReadOnlyList<ReportColumnDto> Columns,
    IReadOnlyList<FilterType> SupportedFilters);
