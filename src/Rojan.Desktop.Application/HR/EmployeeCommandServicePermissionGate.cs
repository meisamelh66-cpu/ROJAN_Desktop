using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.HR;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.HrManage"/>.</summary>
public sealed class EmployeeCommandServicePermissionGate : IEmployeeCommandService
{
    private readonly IEmployeeCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public EmployeeCommandServicePermissionGate(IEmployeeCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.CreateEmployeeAsync(request, cancellationToken);
    }

    public Task<EmployeeDto> ActivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.ActivateEmployeeAsync(employeeId, cancellationToken);
    }

    public Task<EmployeeDto> DeactivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.DeactivateEmployeeAsync(employeeId, cancellationToken);
    }

    public Task<EmployeeDto> SuspendEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.SuspendEmployeeAsync(employeeId, cancellationToken);
    }

    public Task<EmployeeDto> AssignDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.HrManage);
        return _inner.AssignDepartmentAsync(employeeId, department, cancellationToken);
    }
}
