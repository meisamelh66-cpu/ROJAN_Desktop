using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IAttendanceQueryService"/> implementation.</summary>
public sealed class AttendanceQueryService : IAttendanceQueryService
{
    private readonly DomainHr.IHrRepository _repository;

    public AttendanceQueryService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AttendanceDto>> GetAttendanceForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var attendance = await _repository.GetAttendanceAsync(cancellationToken).ConfigureAwait(true);
        return attendance.Where(a => a.EmployeeId == employeeId).OrderByDescending(a => a.Date).Select(HrMapper.MapAttendance).ToList();
    }

    public async Task<IReadOnlyList<AttendanceDto>> GetTodayAttendanceAsync(CancellationToken cancellationToken = default)
    {
        var attendance = await _repository.GetAttendanceAsync(cancellationToken).ConfigureAwait(true);
        var today = DateOnly.FromDateTime(DateTime.Now);
        return attendance.Where(a => a.Date == today).Select(HrMapper.MapAttendance).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsAsync(CancellationToken cancellationToken = default)
    {
        var leaveRequests = await _repository.GetLeaveRequestsAsync(cancellationToken).ConfigureAwait(true);
        return leaveRequests.OrderByDescending(l => l.RequestedAt).Select(HrMapper.MapLeaveRequest).ToList();
    }
}
