namespace Rojan.Desktop.Domain.HR;

/// <summary>
/// The employee's job function - not one of Phase 19's explicitly
/// enumerated enums, but "Employee" naming a role only means something if
/// it actually carries one, so this is added as the natural role
/// vocabulary for a salon staff roster.
/// </summary>
public enum EmployeeRole
{
    Stylist,
    Colorist,
    NailTechnician,
    Esthetician,
    MassageTherapist,
    Receptionist,
    Manager,
}
