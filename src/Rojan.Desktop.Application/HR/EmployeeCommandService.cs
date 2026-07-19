using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IEmployeeCommandService"/> implementation - enforces <c>Domain.HR.EmployeeStatusRules</c> before every status transition, same validation-enforcement pattern as every other command service in this app.</summary>
public sealed class EmployeeCommandService : IEmployeeCommandService
{
    private readonly DomainHr.IHrRepository _repository;

    public EmployeeCommandService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = new DomainHr.Employee(
            Guid.NewGuid().ToString(),
            request.SpecialistId,
            request.FullName,
            request.Email,
            request.Phone,
            HrMapper.MapRoleToDomain(request.Role),
            HrMapper.MapDepartmentToDomain(request.Department),
            HrMapper.MapEmploymentTypeToDomain(request.EmploymentType),
            DomainHr.EmployeeStatus.Active,
            request.HireDate,
            request.BaseSalary);

        var created = await _repository.CreateEmployeeAsync(employee, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapEmployee(created);
    }

    public async Task<EmployeeDto> ActivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetRequiredEmployeeAsync(employeeId, cancellationToken).ConfigureAwait(true);
        if (!DomainHr.EmployeeStatusRules.CanActivate(employee.Status))
        {
            throw new InvalidOperationException($"Employee '{employeeId}' cannot be activated from status '{employee.Status}'.");
        }

        var updated = await _repository.UpdateEmployeeStatusAsync(employeeId, DomainHr.EmployeeStatus.Active, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapEmployee(updated);
    }

    public async Task<EmployeeDto> DeactivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetRequiredEmployeeAsync(employeeId, cancellationToken).ConfigureAwait(true);
        if (!DomainHr.EmployeeStatusRules.CanDeactivate(employee.Status))
        {
            throw new InvalidOperationException($"Employee '{employeeId}' cannot be deactivated from status '{employee.Status}'.");
        }

        var updated = await _repository.UpdateEmployeeStatusAsync(employeeId, DomainHr.EmployeeStatus.Inactive, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapEmployee(updated);
    }

    public async Task<EmployeeDto> SuspendEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetRequiredEmployeeAsync(employeeId, cancellationToken).ConfigureAwait(true);
        if (!DomainHr.EmployeeStatusRules.CanSuspend(employee.Status))
        {
            throw new InvalidOperationException($"Employee '{employeeId}' cannot be suspended from status '{employee.Status}'.");
        }

        var updated = await _repository.UpdateEmployeeStatusAsync(employeeId, DomainHr.EmployeeStatus.Suspended, cancellationToken).ConfigureAwait(true);
        return HrMapper.MapEmployee(updated);
    }

    public async Task<EmployeeDto> AssignDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateEmployeeDepartmentAsync(employeeId, HrMapper.MapDepartmentToDomain(department), cancellationToken).ConfigureAwait(true);
        return HrMapper.MapEmployee(updated);
    }

    private async Task<DomainHr.Employee> GetRequiredEmployeeAsync(string employeeId, CancellationToken cancellationToken) =>
        await _repository.GetEmployeeByIdAsync(employeeId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Employee '{employeeId}' was not found.");
}
