using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IAttendanceCommandService"/> implementation.</summary>
public sealed class AttendanceCommandService : IAttendanceCommandService
{
    private static readonly TimeSpan GraceWindow = TimeSpan.FromMinutes(10);

    private readonly DomainHr.IHrRepository _repository;

    public AttendanceCommandService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<AttendanceDto> RecordAttendanceAsync(RecordAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        if (!DomainHr.AttendanceRules.IsValidCorrection(request.CheckInTime, request.CheckOutTime))
        {
            throw new ArgumentException("Check-out time must be after check-in time.", nameof(request));
        }

        var employee = await _repository.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Employee '{request.EmployeeId}' was not found.");

        var status = await DetermineStatusAsync(request.EmployeeId, request.Date, request.CheckInTime, request.Status, cancellationToken).ConfigureAwait(true);

        var attendance = new DomainHr.Attendance(
            Guid.NewGuid().ToString(), employee.Id, employee.FullName, request.Date,
            request.CheckInTime, request.CheckOutTime, status, request.Notes);

        var recorded = await _repository.RecordAttendanceAsync(attendance, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapAttendance(recorded);
    }

    public async Task<AttendanceDto> CorrectAttendanceAsync(CorrectAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        if (!DomainHr.AttendanceRules.IsValidCorrection(request.CheckInTime, request.CheckOutTime))
        {
            throw new ArgumentException("Check-out time must be after check-in time.", nameof(request));
        }

        var attendanceRecords = await _repository.GetAttendanceAsync(cancellationToken).ConfigureAwait(true);
        var existing = attendanceRecords.FirstOrDefault(a => a.Id == request.AttendanceId)
            ?? throw new InvalidOperationException($"Attendance record '{request.AttendanceId}' was not found.");

        var corrected = existing with
        {
            CheckInTime = request.CheckInTime,
            CheckOutTime = request.CheckOutTime,
            Status = HrMapper.MapAttendanceStatusToDomain(request.Status),
            Notes = request.Notes,
        };

        var updated = await _repository.UpdateAttendanceAsync(corrected, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapAttendance(updated);
    }

    public async Task<LeaveRequestDto> RequestLeaveAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Employee '{request.EmployeeId}' was not found.");

        var leaveRequest = new DomainHr.LeaveRequest(
            Guid.NewGuid().ToString(), employee.Id, employee.FullName, request.StartDate, request.EndDate,
            request.Reason, DomainHr.LeaveStatus.Pending, DateTimeOffset.Now);

        var created = await _repository.CreateLeaveRequestAsync(leaveRequest, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapLeaveRequest(created);
    }

    public async Task<LeaveRequestDto> ApproveLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateLeaveRequestStatusAsync(leaveRequestId, DomainHr.LeaveStatus.Approved, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapLeaveRequest(updated);
    }

    public async Task<LeaveRequestDto> RejectLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateLeaveRequestStatusAsync(leaveRequestId, DomainHr.LeaveStatus.Rejected, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapLeaveRequest(updated);
    }

    /// <summary>
    /// Explicit <see cref="RecordAttendanceRequest.Status"/> wins; otherwise a
    /// given check-in is compared against the employee's shift start for
    /// that date (if one exists) via <see cref="DomainHr.AttendanceRules.DetermineStatus"/>;
    /// with no shift on record, any check-in counts as
    /// <see cref="DomainHr.AttendanceStatus.Present"/>; with no check-in at
    /// all, the day is <see cref="DomainHr.AttendanceStatus.Absent"/>.
    /// </summary>
    private async Task<DomainHr.AttendanceStatus> DetermineStatusAsync(string employeeId, DateOnly date, TimeSpan? checkInTime, AttendanceStatus? explicitStatus, CancellationToken cancellationToken)
    {
        if (explicitStatus is not null)
        {
            return HrMapper.MapAttendanceStatusToDomain(explicitStatus.Value);
        }

        if (checkInTime is null)
        {
            return DomainHr.AttendanceStatus.Absent;
        }

        var assignments = await _repository.GetShiftAssignmentsAsync(cancellationToken).ConfigureAwait(true);
        var assignment = assignments.FirstOrDefault(a => a.EmployeeId == employeeId && a.AssignedDate == date);
        if (assignment is null)
        {
            return DomainHr.AttendanceStatus.Present;
        }

        var shifts = await _repository.GetShiftsAsync(cancellationToken).ConfigureAwait(true);
        var shift = shifts.FirstOrDefault(s => s.Id == assignment.ShiftId);
        if (shift is null)
        {
            return DomainHr.AttendanceStatus.Present;
        }

        return DomainHr.AttendanceRules.DetermineStatus(shift.StartTime, checkInTime.Value, GraceWindow);
    }
}
