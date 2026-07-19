namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of an employee, mapped from <see cref="Rojan.Desktop.Domain.HR.Employee"/> by <see cref="HrMapper"/>.</summary>
public sealed record EmployeeDto(
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
