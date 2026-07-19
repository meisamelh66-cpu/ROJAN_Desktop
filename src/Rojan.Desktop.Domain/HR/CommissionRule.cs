namespace Rojan.Desktop.Domain.HR;

/// <summary><see cref="Value"/> is a fraction (0.10 = 10%) when <see cref="Type"/> is <see cref="CommissionType.Percentage"/>, or a flat dollar amount when <see cref="CommissionType.FixedAmount"/>.</summary>
public sealed record CommissionRule(
    string Id,
    string EmployeeId,
    string EmployeeName,
    CommissionType Type,
    decimal Value,
    string Description);
