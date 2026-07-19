namespace Rojan.Desktop.Application.HR;

public sealed record CreateCommissionRuleRequest(
    string EmployeeId,
    CommissionType Type,
    decimal Value,
    string Description);
