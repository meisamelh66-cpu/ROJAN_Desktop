namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of extended HR profile data, mapped from <see cref="Rojan.Desktop.Domain.HR.EmployeeProfile"/> by <see cref="HrMapper"/>.</summary>
public sealed record EmployeeDetailDto(
    string Id,
    string EmployeeId,
    string Bio,
    string Skills,
    string EmergencyContactName,
    string EmergencyContactPhone);
