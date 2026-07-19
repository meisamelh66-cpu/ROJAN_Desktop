namespace Rojan.Desktop.Domain.HR;

/// <summary>
/// A single staff member. <see cref="SpecialistId"/> is a free-text
/// cross-slice reference to <c>Domain.Specialists.Specialist</c> - empty
/// for staff who don't perform bookable services (reception, management)
/// - same "consistent naming, not a real FK" cross-slice reasoning as
/// every other free-text reference in this app (see
/// <c>Domain.Specialists.Specialist</c>'s own doc comment). Money uses
/// <c>decimal</c>, not the display-only string-money convention most
/// other modules use - same justified departure
/// <c>Domain.Accounting.Invoice</c> made, since payroll requires genuine
/// arithmetic.
/// </summary>
public sealed record Employee(
    string Id,
    string SpecialistId,
    string FullName,
    string Email,
    string Phone,
    EmployeeRole Role,
    Department Department,
    EmploymentType EmploymentType,
    EmployeeStatus Status,
    DateOnly HireDate,
    decimal BaseSalary);
