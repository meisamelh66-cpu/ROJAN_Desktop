namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of a leave request, mapped from <see cref="Rojan.Desktop.Domain.HR.LeaveRequest"/> by <see cref="HrMapper"/>.</summary>
public sealed record LeaveRequestDto(
    string Id,
    string EmployeeId,
    string EmployeeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    LeaveStatus Status,
    DateTimeOffset RequestedAt);
