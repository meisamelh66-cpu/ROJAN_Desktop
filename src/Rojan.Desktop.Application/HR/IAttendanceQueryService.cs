namespace Rojan.Desktop.Application.HR;

/// <summary>Read-only use cases Presentation depends on to load Attendance.</summary>
public interface IAttendanceQueryService
{
    public Task<IReadOnlyList<AttendanceDto>> GetAttendanceForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>Every employee's attendance record for today - backs the Attendance page's daily roster and the Dashboard's Present/Late KPIs.</summary>
    public Task<IReadOnlyList<AttendanceDto>> GetTodayAttendanceAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsAsync(CancellationToken cancellationToken = default);
}
