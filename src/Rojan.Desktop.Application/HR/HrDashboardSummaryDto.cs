namespace Rojan.Desktop.Application.HR;

/// <summary>The HR Dashboard's KPI card numbers - composed in Application over Employees/Attendance/Payroll/Commission, same "read the set, compose in Application" convention every other module's derived numbers follow.</summary>
public sealed record HrDashboardSummaryDto(
    int EmployeeCount,
    int PresentToday,
    int LateToday,
    int OnLeaveCount,
    decimal PayrollThisMonth,
    decimal CommissionThisMonth,
    decimal AverageAttendancePercent);
