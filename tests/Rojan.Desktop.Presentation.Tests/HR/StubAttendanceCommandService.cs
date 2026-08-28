using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

internal sealed class StubAttendanceCommandService : IAttendanceCommandService
{
    public List<RecordAttendanceRequest> RecordRequests { get; } = [];

    public List<CreateLeaveRequestRequest> LeaveRequests { get; } = [];

    public List<string> ApprovedLeaveIds { get; } = [];

    public List<string> RejectedLeaveIds { get; } = [];

    /// <summary>Production Hardening (missing-guard sweep, Wave B): when set, the matching command throws this instead of succeeding. Same seam pattern as Customers.StubCustomerCommandService.CreateCustomerException. The call is still recorded before the throw.</summary>
    public Exception? RecordAttendanceException { get; set; }

    public Exception? RequestLeaveException { get; set; }

    public Exception? ApproveLeaveException { get; set; }

    public Exception? RejectLeaveException { get; set; }

    public Task<AttendanceDto> RecordAttendanceAsync(RecordAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        RecordRequests.Add(request);
        if (RecordAttendanceException is not null)
        {
            return Task.FromException<AttendanceDto>(RecordAttendanceException);
        }

        return Task.FromResult(new AttendanceDto("attendance-new", request.EmployeeId, "Test Employee", request.Date, request.CheckInTime, request.CheckOutTime, request.Status ?? AttendanceStatus.Present, request.Notes));
    }

    public Task<AttendanceDto> CorrectAttendanceAsync(CorrectAttendanceRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AttendanceDto(request.AttendanceId, "employee-1", "Test Employee", DateOnly.FromDateTime(DateTime.Today), request.CheckInTime, request.CheckOutTime, request.Status, request.Notes));

    public Task<LeaveRequestDto> RequestLeaveAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        LeaveRequests.Add(request);
        return RequestLeaveException is not null
            ? Task.FromException<LeaveRequestDto>(RequestLeaveException)
            : Task.FromResult(new LeaveRequestDto("leave-new", request.EmployeeId, "Test Employee", request.StartDate, request.EndDate, request.Reason, LeaveStatus.Pending, DateTimeOffset.Now));
    }

    public Task<LeaveRequestDto> ApproveLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        ApprovedLeaveIds.Add(leaveRequestId);
        return ApproveLeaveException is not null
            ? Task.FromException<LeaveRequestDto>(ApproveLeaveException)
            : Task.FromResult(new LeaveRequestDto(leaveRequestId, "employee-1", "Test Employee", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), string.Empty, LeaveStatus.Approved, DateTimeOffset.Now));
    }

    public Task<LeaveRequestDto> RejectLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        RejectedLeaveIds.Add(leaveRequestId);
        return RejectLeaveException is not null
            ? Task.FromException<LeaveRequestDto>(RejectLeaveException)
            : Task.FromResult(new LeaveRequestDto(leaveRequestId, "employee-1", "Test Employee", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), string.Empty, LeaveStatus.Rejected, DateTimeOffset.Now));
    }
}
