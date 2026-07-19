namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of an attendance record, mapped from <see cref="Rojan.Desktop.Domain.HR.Attendance"/> by <see cref="HrMapper"/>.</summary>
public sealed record AttendanceDto(
    string Id,
    string EmployeeId,
    string EmployeeName,
    DateOnly Date,
    TimeSpan? CheckInTime,
    TimeSpan? CheckOutTime,
    AttendanceStatus Status,
    string Notes);
