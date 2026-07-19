namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of a commission rule, mapped from <see cref="Rojan.Desktop.Domain.HR.CommissionRule"/> by <see cref="HrMapper"/>.</summary>
public sealed record CommissionRuleDto(
    string Id,
    string EmployeeId,
    string EmployeeName,
    CommissionType Type,
    decimal Value,
    string Description);
