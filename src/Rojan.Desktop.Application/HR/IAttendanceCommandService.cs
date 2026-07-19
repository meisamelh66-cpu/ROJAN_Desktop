namespace Rojan.Desktop.Application.HR;

/// <summary>Write use cases for Attendance and Leave - registration, correction, leave requesting, and leave approval/rejection.</summary>
public interface IAttendanceCommandService
{
    /// <summary>Records attendance, deriving <see cref="AttendanceDto.Status"/> from the employee's shift via <c>Domain.HR.AttendanceRules.DetermineStatus</c> when not explicitly given.</summary>
    public Task<AttendanceDto> RecordAttendanceAsync(RecordAttendanceRequest request, CancellationToken cancellationToken = default);

    public Task<AttendanceDto> CorrectAttendanceAsync(CorrectAttendanceRequest request, CancellationToken cancellationToken = default);

    public Task<LeaveRequestDto> RequestLeaveAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default);

    public Task<LeaveRequestDto> ApproveLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default);

    public Task<LeaveRequestDto> RejectLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default);
}
