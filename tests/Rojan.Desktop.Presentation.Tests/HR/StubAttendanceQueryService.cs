using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubAttendanceQueryService : IAttendanceQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<AttendanceDto>>>? _getTodayAttendance;
    private readonly Func<CancellationToken, Task<IReadOnlyList<LeaveRequestDto>>>? _getLeaveRequests;

    public StubAttendanceQueryService(
        Func<CancellationToken, Task<IReadOnlyList<AttendanceDto>>>? getTodayAttendance = null,
        Func<CancellationToken, Task<IReadOnlyList<LeaveRequestDto>>>? getLeaveRequests = null)
    {
        _getTodayAttendance = getTodayAttendance;
        _getLeaveRequests = getLeaveRequests;
    }

    public Task<IReadOnlyList<AttendanceDto>> GetAttendanceForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AttendanceDto>>([]);

    public Task<IReadOnlyList<AttendanceDto>> GetTodayAttendanceAsync(CancellationToken cancellationToken = default) =>
        _getTodayAttendance?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<AttendanceDto>>([]);

    public Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsAsync(CancellationToken cancellationToken = default) =>
        _getLeaveRequests?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<LeaveRequestDto>>([]);
}
