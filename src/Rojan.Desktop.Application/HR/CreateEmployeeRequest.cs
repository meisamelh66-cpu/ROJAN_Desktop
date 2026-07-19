namespace Rojan.Desktop.Application.HR;

public sealed record CreateEmployeeRequest(
    string SpecialistId,
    string FullName,
    string Email,
    string Phone,
    EmployeeRole Role,
    Department Department,
    EmploymentType EmploymentType,
    DateOnly HireDate,
    decimal BaseSalary);
