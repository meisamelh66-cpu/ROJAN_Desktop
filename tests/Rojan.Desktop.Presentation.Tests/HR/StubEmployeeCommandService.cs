using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Inventory.StubInventoryCommandService.</summary>
internal sealed class StubEmployeeCommandService : IEmployeeCommandService
{
    public List<CreateEmployeeRequest> CreateRequests { get; } = [];

    public List<string> ActivatedIds { get; } = [];

    public List<string> DeactivatedIds { get; } = [];

    public List<string> SuspendedIds { get; } = [];

    private static EmployeeDto MakeDto(string id, EmployeeStatus status) =>
        new(id, string.Empty, "Test Employee", "test@rojan.example", "+1 555", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, status, DateOnly.FromDateTime(DateTime.Today), 2500m);

    public Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        return Task.FromResult(new EmployeeDto("employee-new", string.Empty, request.FullName, request.Email, request.Phone, request.Role, request.Department, request.EmploymentType, EmployeeStatus.Active, request.HireDate, request.BaseSalary));
    }

    public Task<EmployeeDto> ActivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        ActivatedIds.Add(employeeId);
        return Task.FromResult(MakeDto(employeeId, EmployeeStatus.Active));
    }

    public Task<EmployeeDto> DeactivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        DeactivatedIds.Add(employeeId);
        return Task.FromResult(MakeDto(employeeId, EmployeeStatus.Inactive));
    }

    public Task<EmployeeDto> SuspendEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        SuspendedIds.Add(employeeId);
        return Task.FromResult(MakeDto(employeeId, EmployeeStatus.Suspended));
    }

    public Task<EmployeeDto> AssignDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default) =>
        Task.FromResult(MakeDto(employeeId, EmployeeStatus.Active) with { Department = department });
}
