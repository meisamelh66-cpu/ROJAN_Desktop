namespace Rojan.Desktop.Application.Reporting;

/// <summary>Application's own copy of the report-type concept, distinct from <see cref="Rojan.Desktop.Domain.Reporting.ReportType"/> - Presentation only ever sees this one. <see cref="ReportingMapper"/> maps between the two explicitly.</summary>
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
