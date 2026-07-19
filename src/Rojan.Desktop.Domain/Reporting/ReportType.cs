namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// Every report the Reporting platform can generate. Each value maps to
/// exactly one aggregation branch in <c>Application.Reporting.ReportExecutionQueryService</c>.
/// </summary>
public enum ReportType
{
    RevenueReport,
    SalesReport,
    AppointmentsReport,
    CustomerGrowth,
    CustomerRetention,
    ServicePopularity,
    SpecialistPerformance,
    InventoryValuation,
    LowStock,
    PayrollSummary,
    CommissionSummary,
    AttendanceSummary,
    DailyDashboard,
    WeeklyDashboard,
    MonthlyDashboard,
}
