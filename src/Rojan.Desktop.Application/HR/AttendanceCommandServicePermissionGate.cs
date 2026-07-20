using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.HR;

/// <summary>
/// Phase 22A: Enterprise Context Migration - same "wrap the real service
/// with permission enforcement" pattern as
/// <c>Customers.CustomerCommandServicePermissionGate</c>. Recording/
/// correcting attendance and requesting leave require
/// <see cref="Permission.HrManage"/>; approving/rejecting a leave request
/// use the dedicated <see cref="Permission.Approve"/>/<see cref="Permission.Reject"/>
/// action-level permissions this phase added.
/// </summary>
public sealed class AttendanceCommandServicePermissionGate : IAttendanceCommandService
{
    private readonly IAttendanceCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public AttendanceCommandServicePermissionGate(IAttendanceCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<AttendanceDto> RecordAttendanceAsync(RecordAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.RecordAttendanceAsync(request, cancellationToken);
    }

    public Task<AttendanceDto> CorrectAttendanceAsync(CorrectAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.CorrectAttendanceAsync(request, cancellationToken);
    }

    public Task<LeaveRequestDto> RequestLeaveAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.RequestLeaveAsync(request, cancellationToken);
    }

    public Task<LeaveRequestDto> ApproveLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.Approve);
        return _inner.ApproveLeaveAsync(leaveRequestId, cancellationToken);
    }

    public Task<LeaveRequestDto> RejectLeaveAsync(string leaveRequestId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.Reject);
        return _inner.RejectLeaveAsync(leaveRequestId, cancellationToken);
    }
}
