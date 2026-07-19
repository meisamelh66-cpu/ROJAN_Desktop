namespace Rojan.Desktop.Domain.HR;

/// <summary>Attendance registration/correction rules - a genuine Domain rule, same reasoning as every other <c>*Rules</c> class in this app.</summary>
public static class AttendanceRules
{
    /// <summary>A check-in at or before the shift's start plus <paramref name="graceWindow"/> is <see cref="AttendanceStatus.Present"/>; anything later is <see cref="AttendanceStatus.Late"/>.</summary>
    public static AttendanceStatus DetermineStatus(TimeSpan shiftStart, TimeSpan checkInTime, TimeSpan graceWindow) =>
        checkInTime <= shiftStart + graceWindow ? AttendanceStatus.Present : AttendanceStatus.Late;

    /// <summary>A correction is valid when there is no check-out, or the check-out is strictly after the check-in.</summary>
    public static bool IsValidCorrection(TimeSpan? checkInTime, TimeSpan? checkOutTime) =>
        checkOutTime is null || checkInTime is null || checkOutTime > checkInTime;
}
