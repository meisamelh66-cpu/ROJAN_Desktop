namespace Rojan.Desktop.Application.HR;

/// <summary>Write use cases for Employees - creation and lifecycle transitions (activate/deactivate/suspend), plus department assignment.</summary>
public interface IEmployeeCommandService
{
    public Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    public Task<EmployeeDto> ActivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);

    public Task<EmployeeDto> DeactivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);

    public Task<EmployeeDto> SuspendEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);

    public Task<EmployeeDto> AssignDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default);
}
