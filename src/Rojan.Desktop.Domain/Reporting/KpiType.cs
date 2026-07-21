namespace Rojan.Desktop.Domain.Reporting;

/// <summary>The KPI Engine's supported metric families - each has its own aggregation branch in <c>Application.Reporting.KpiEngineQueryService</c>.</summary>
public enum KpiType
{
    Revenue,
    Appointments,
    Customers,
    Inventory,
    Payroll,
    Attendance,
    Growth,
    Trend,

    // Phase 33: Enterprise Reporting & Business Intelligence Platform.
    Profit,
    Expenses,
    CancellationRate,
    AverageTicket,
    AverageServiceTime,
    Retention,
    EmployeeProductivity,
}
