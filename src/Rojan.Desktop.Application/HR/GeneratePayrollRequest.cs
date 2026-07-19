namespace Rojan.Desktop.Application.HR;

public sealed record GeneratePayrollRequest(
    string EmployeeId,
    int Month,
    int Year,
    decimal Bonus,
    decimal Deduction);
