namespace Rojan.Desktop.Application.HR;

/// <summary>Application's own copy of <see cref="Rojan.Desktop.Domain.HR.EmployeeStatus"/> - distinct from Domain, same reasoning as every other Application-layer enum copy in this app: Presentation never binds to a Domain-shaped type.</summary>
public enum EmployeeStatus
{
    Active,
    Inactive,
    Suspended,
    OnLeave,
}
