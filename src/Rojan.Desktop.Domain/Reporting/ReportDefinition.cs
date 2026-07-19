namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// Catalog entry describing one runnable report - the Report Viewer lists
/// these, the user picks one (plus filters), and
/// <c>Application.Reporting.IReportExecutionQueryService.RunReportAsync</c>
/// produces a <see cref="ReportResult"/> for it. All fifteen ship
/// system-defined (<see cref="IsSystemDefined"/> true) and seeded by
/// <c>Infrastructure.Reporting.FakeReportingRepository</c> - there is no
/// custom report builder in this phase.
/// </summary>
public sealed record ReportDefinition(
    string Id,
    string Name,
    string Description,
    ReportType ReportType,
    ReportCategory Category,
    bool IsSystemDefined,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<FilterType> SupportedFilters);
