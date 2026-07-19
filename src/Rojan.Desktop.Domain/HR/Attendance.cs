namespace Rojan.Desktop.Domain.HR;

/// <summary>One employee's attendance record for one date - check-in/out times are nullable since a record can be created (e.g. as "Absent") before any check-in ever happens.</summary>
public sealed record Attendance(
    string Id,
    string EmployeeId,
    string EmployeeName,
    DateOnly Date,
    TimeSpan? CheckInTime,
    TimeSpan? CheckOutTime,
    AttendanceStatus Status,
    string Notes);
