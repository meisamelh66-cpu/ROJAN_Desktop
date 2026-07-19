namespace Rojan.Desktop.Presentation.ViewModels.HR;

/// <summary>
/// The HR page's switchable content sections - Dashboard KPIs render
/// always at the top of the page (same "always-visible summary" shape
/// as every other module's KPI row), while Employees/Attendance/Shifts/
/// Leave/Commission/Payroll are one-at-a-time sections below it, picked
/// via a local section switcher - no new navigation surface, no new
/// Design System components.
/// </summary>
public enum HrSection
{
    Employees,
    Attendance,
    Shifts,
    Leave,
    Commission,
    Payroll,
}
