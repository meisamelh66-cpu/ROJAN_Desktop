using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Inventory.StubInventoryCommandService.</summary>
internal sealed class StubEmployeeCommandService : IEmployeeCommandService
{
    public List<CreateEmployeeRequest> CreateRequests { get; } = [];

    public List<string> ActivatedIds { get; } = [];

    public List<string> DeactivatedIds { get; } = [];

    public List<string> SuspendedIds { get; } = [];

    /// <summary>Production Hardening (missing-guard sweep, Wave B): when set, the matching command throws this instead of succeeding - lets a test exercise the ViewModel's new try/catch without a real backend failure. Same seam pattern as Customers.StubCustomerCommandService.CreateCustomerException. The call is still recorded before the throw.</summary>
    public Exception? CreateEmployeeException { get; set; }

    public Exception? ActivateEmployeeException { get; set; }

    public Exception? DeactivateEmployeeException { get; set; }

    public Exception? SuspendEmployeeException { get; set; }

    private static EmployeeDto MakeDto(string id, EmployeeStatus status) =>
        new(id, string.Empty, "Test Employee", "test@rojan.example", "+1 555", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, status, DateOnly.FromDateTime(DateTime.Today), 2500m);

    public Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        if (CreateEmployeeException is not null)
        {
            return Task.FromException<EmployeeDto>(CreateEmployeeException);
        }

        return Task.FromResult(new EmployeeDto("employee-new", string.Empty, request.FullName, request.Email, request.Phone, request.Role, request.Department, request.EmploymentType, EmployeeStatus.Active, request.HireDate, request.BaseSalary));
    }

    public Task<EmployeeDto> ActivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        ActivatedIds.Add(employeeId);
        return ActivateEmployeeException is not null
            ? Task.FromException<EmployeeDto>(ActivateEmployeeException)
            : Task.FromResult(MakeDto(employeeId, EmployeeStatus.Active));
    }

    public Task<EmployeeDto> DeactivateEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        DeactivatedIds.Add(employeeId);
        return DeactivateEmployeeException is not null
            ? Task.FromException<EmployeeDto>(DeactivateEmployeeException)
            : Task.FromResult(MakeDto(employeeId, EmployeeStatus.Inactive));
    }

    public Task<EmployeeDto> SuspendEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        SuspendedIds.Add(employeeId);
        return SuspendEmployeeException is not null
            ? Task.FromException<EmployeeDto>(SuspendEmployeeException)
            : Task.FromResult(MakeDto(employeeId, EmployeeStatus.Suspended));
    }

    public Task<EmployeeDto> AssignDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default) =>
        Task.FromResult(MakeDto(employeeId, EmployeeStatus.Active) with { Department = department });
}
