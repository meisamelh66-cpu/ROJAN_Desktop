namespace Rojan.Desktop.Domain.HR;

/// <summary>Foundation-only monthly payroll summary for one employee - Base Salary + Commission + Bonus - Deduction = Net Salary (see <see cref="PayrollCalculator"/>). No government payroll, no tax engine, no accounting export - a deliberate scope limit, same "foundation now" pattern as every other module's first pass.</summary>
public sealed record PayrollSummary(
    string Id,
    string EmployeeId,
    string EmployeeName,
    int Month,
    int Year,
    decimal BaseSalary,
    decimal CommissionTotal,
    decimal Bonus,
    decimal Deduction,
    decimal NetSalary,
    DateTimeOffset GeneratedAt);
