namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of a payroll summary, mapped from <see cref="Rojan.Desktop.Domain.HR.PayrollSummary"/> by <see cref="HrMapper"/>.</summary>
public sealed record PayrollSummaryDto(
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
