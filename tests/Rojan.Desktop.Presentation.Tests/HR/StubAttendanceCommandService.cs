using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubAttendanceCommandService : IAttendanceCommandService
{
    public List<RecordAttendanceRequest> RecordRequests { get; } = [];

    public List<CreateLeaveRequestRequest> LeaveRequests { get; } = [];

    public List<string> ApprovedLeaveIds { get; } = [];

    public List<string> RejectedLeaveIds { get; } = [];

    public Task<AttendanceDto> RecordAttendanceAsync(RecordAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        RecordRequests.Add(request);
        return Task.FromResult(new AttendanceDto("attendance-new", request.EmployeeId, "Test Employee", request.Date, request.CheckInTime, request.CheckOutTime, request.Status ?? AttendanceStatus.Present, request.Notes));
    }

    public Task<AttendanceDto> CorrectAttendanceAsync(CorrectAttendanceRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AttendanceDto(request.AttendanceId, "employee-1", "Test Employee", DateOnly.FromDateTime(DateTime.Today), request.CheckInTime, request.CheckOutTime, request.Status, request.Notes));

    public Task<LeaveRequestDto> RequestLeaveAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        LeaveRequests.Add(request);
        return Task.FromResult(new LeaveRequestDto("leave-new", request.EmployeeId, "Test Employee", request.StartDate, request.EndDate, request.Reason, LeaveStatus.Pending, DateTimeOffset.Now));
    }

    public Task<LeaveRequestDto> ApproveLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        ApprovedLeaveIds.Add(leaveRequestId);
        return Task.FromResult(new LeaveRequestDto(leaveRequestId, "employee-1", "Test Employee", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), string.Empty, LeaveStatus.Approved, DateTimeOffset.Now));
    }

    public Task<LeaveRequestDto> RejectLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        RejectedLeaveIds.Add(leaveRequestId);
        return Task.FromResult(new LeaveRequestDto(leaveRequestId, "employee-1", "Test Employee", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), string.Empty, LeaveStatus.Rejected, DateTimeOffset.Now));
    }
}
