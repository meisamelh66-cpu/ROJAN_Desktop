namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of a shift assignment, mapped from <see cref="Rojan.Desktop.Domain.HR.ShiftAssignment"/> by <see cref="HrMapper"/>.</summary>
public sealed record ShiftAssignmentDto(
    string Id,
    string ShiftId,
    string EmployeeId,
    string EmployeeName,
    DateOnly AssignedDate);
