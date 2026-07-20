using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.HR;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.HrManage"/>.</summary>
public sealed class ShiftCommandServicePermissionGate : IShiftCommandService
{
    private readonly IShiftCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public ShiftCommandServicePermissionGate(IShiftCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.CreateShiftAsync(request, cancellationToken);
    }

    public Task<ShiftAssignmentDto> AssignShiftAsync(AssignShiftRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.AssignShiftAsync(request, cancellationToken);
    }
}
