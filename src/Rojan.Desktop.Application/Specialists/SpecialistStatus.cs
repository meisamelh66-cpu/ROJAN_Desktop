namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Application's own copy of <see cref="Rojan.Desktop.Domain.Specialists.SpecialistStatus"/> -
/// distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>:
/// Presentation never binds to a Domain-shaped type, so anything it needs
/// gets an Application-owned equivalent, mapped explicitly by
/// <see cref="SpecialistMapper"/>.
/// </summary>
public enum SpecialistStatus
{
    Active,
    OnLeave,
    Inactive,
}
