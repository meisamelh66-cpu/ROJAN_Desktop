using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// Domain&lt;-&gt;Application mapping for everything Reporting genuinely owns
/// (<see cref="DomainReporting.ReportDefinition"/>, <see cref="DomainReporting.ReportSnapshot"/>)
/// plus the enum-mirroring helpers every cross-boundary Reporting type
/// needs. Internal - only this project's own services call it, matching
/// every other module's Mapper convention (<c>CustomerMapper</c>).
/// </summary>
internal static class ReportingMapper
{
    public static ReportColumnDto MapColumn(DomainReporting.ReportColumn column) =>
        new(column.Key, column.Header, MapDataType(column.DataType));

    public static ReportDefinitionDto MapDefinition(DomainReporting.ReportDefinition definition) => new(
        definition.Id,
        definition.Name,
        definition.Description,
        MapReportType(definition.ReportType),
        MapCategory(definition.Category),
        definition.IsSystemDefined,
        definition.Columns.Select(MapColumn).ToList(),
        definition.SupportedFilters.Select(MapFilterType).ToList());

    public static ReportSnapshotDto MapSnapshot(DomainReporting.ReportSnapshot snapshot) => new(
        snapshot.Id,
        snapshot.ReportDefinitionId,
        snapshot.ReportName,
        snapshot.GeneratedAt,
        snapshot.AppliedFilters.Select(MapFilter).ToList(),
        snapshot.RowCount,
        snapshot.IsSaved);

    public static ReportFilterDto MapFilter(DomainReporting.ReportFilter filter) =>
        new(MapFilterType(filter.FilterType), filter.Value, filter.DisplayLabel);

    public static DomainReporting.ReportFilter MapFilter(ReportFilterDto filter) =>
        new(MapFilterType(filter.FilterType), filter.Value, filter.DisplayLabel);

    public static ReportType MapReportType(DomainReporting.ReportType reportType) => reportType switch
    {
        DomainReporting.ReportType.RevenueReport => ReportType.RevenueReport,
        DomainReporting.ReportType.SalesReport => ReportType.SalesReport,
        DomainReporting.ReportType.AppointmentsReport => ReportType.AppointmentsReport,
        DomainReporting.ReportType.CustomerGrowth => ReportType.CustomerGrowth,
        DomainReporting.ReportType.CustomerRetention => ReportType.CustomerRetention,
        DomainReporting.ReportType.ServicePopularity => ReportType.ServicePopularity,
        DomainReporting.ReportType.SpecialistPerformance => ReportType.SpecialistPerformance,
        DomainReporting.ReportType.InventoryValuation => ReportType.InventoryValuation,
        DomainReporting.ReportType.LowStock => ReportType.LowStock,
        DomainReporting.ReportType.PayrollSummary => ReportType.PayrollSummary,
        DomainReporting.ReportType.CommissionSummary => ReportType.CommissionSummary,
        DomainReporting.ReportType.AttendanceSummary => ReportType.AttendanceSummary,
        DomainReporting.ReportType.DailyDashboard => ReportType.DailyDashboard,
        DomainReporting.ReportType.WeeklyDashboard => ReportType.WeeklyDashboard,
        DomainReporting.ReportType.MonthlyDashboard => ReportType.MonthlyDashboard,
        _ => throw new ArgumentOutOfRangeException(nameof(reportType), reportType, "Unknown report type."),
    };

    public static ReportCategory MapCategory(DomainReporting.ReportCategory category) => category switch
    {
        DomainReporting.ReportCategory.Financial => ReportCategory.Financial,
        DomainReporting.ReportCategory.Operations => ReportCategory.Operations,
        DomainReporting.ReportCategory.Customers => ReportCategory.Customers,
        DomainReporting.ReportCategory.Workforce => ReportCategory.Workforce,
        DomainReporting.ReportCategory.Dashboards => ReportCategory.Dashboards,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown report category."),
    };

    public static ReportColumnDataType MapDataType(DomainReporting.ReportColumnDataType dataType) => dataType switch
    {
        DomainReporting.ReportColumnDataType.Text => ReportColumnDataType.Text,
        DomainReporting.ReportColumnDataType.Number => ReportColumnDataType.Number,
        DomainReporting.ReportColumnDataType.Currency => ReportColumnDataType.Currency,
        DomainReporting.ReportColumnDataType.Percentage => ReportColumnDataType.Percentage,
        DomainReporting.ReportColumnDataType.Date => ReportColumnDataType.Date,
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unknown column data type."),
    };

    public static FilterType MapFilterType(DomainReporting.FilterType filterType) => filterType switch
    {
        DomainReporting.FilterType.DateRange => FilterType.DateRange,
        DomainReporting.FilterType.Branch => FilterType.Branch,
        DomainReporting.FilterType.Employee => FilterType.Employee,
        DomainReporting.FilterType.Specialist => FilterType.Specialist,
        DomainReporting.FilterType.Service => FilterType.Service,
        DomainReporting.FilterType.Customer => FilterType.Customer,
        DomainReporting.FilterType.Status => FilterType.Status,
        DomainReporting.FilterType.Category => FilterType.Category,
        _ => throw new ArgumentOutOfRangeException(nameof(filterType), filterType, "Unknown filter type."),
    };

    public static DomainReporting.FilterType MapFilterType(FilterType filterType) => filterType switch
    {
        FilterType.DateRange => DomainReporting.FilterType.DateRange,
        FilterType.Branch => DomainReporting.FilterType.Branch,
        FilterType.Employee => DomainReporting.FilterType.Employee,
        FilterType.Specialist => DomainReporting.FilterType.Specialist,
        FilterType.Service => DomainReporting.FilterType.Service,
        FilterType.Customer => DomainReporting.FilterType.Customer,
        FilterType.Status => DomainReporting.FilterType.Status,
        FilterType.Category => DomainReporting.FilterType.Category,
        _ => throw new ArgumentOutOfRangeException(nameof(filterType), filterType, "Unknown filter type."),
    };

    public static TrendDirection MapTrend(DomainReporting.TrendDirection trend) => trend switch
    {
        DomainReporting.TrendDirection.Flat => TrendDirection.Flat,
        DomainReporting.TrendDirection.Up => TrendDirection.Up,
        DomainReporting.TrendDirection.Down => TrendDirection.Down,
        _ => throw new ArgumentOutOfRangeException(nameof(trend), trend, "Unknown trend direction."),
    };
}
