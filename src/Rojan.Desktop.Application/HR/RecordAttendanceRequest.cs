namespace Rojan.Desktop.Application.HR;

/// <summary>
/// <see cref="Status"/> is optional: when omitted and <see cref="CheckInTime"/>
/// is given, <c>AttendanceCommandService</c> derives it from the
/// employee's shift for that date via
/// <c>Domain.HR.AttendanceRules.DetermineStatus</c>; when omitted with no
/// check-in, it defaults to <see cref="AttendanceStatus.Absent"/>.
/// </summary>
public sealed record RecordAttendanceRequest(
    string EmployeeId,
    DateOnly Date,
    TimeSpan? CheckInTime,
    TimeSpan? CheckOutTime,
    AttendanceStatus? Status,
    string Notes);
