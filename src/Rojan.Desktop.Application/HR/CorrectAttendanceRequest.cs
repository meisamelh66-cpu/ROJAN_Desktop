namespace Rojan.Desktop.Application.HR;

public sealed record CorrectAttendanceRequest(
    string AttendanceId,
    TimeSpan? CheckInTime,
    TimeSpan? CheckOutTime,
    AttendanceStatus Status,
    string Notes);
