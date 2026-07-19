namespace Rojan.Desktop.Application.HR;

/// <summary>Read-only use cases Presentation depends on to load Employees - the only way Presentation ever reaches employee data, never through Domain/Infrastructure directly.</summary>
public interface IEmployeeQueryService
{
    public Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns employees whose name, email, role, or department contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every employee.</summary>
    public Task<IReadOnlyList<EmployeeDto>> SearchEmployeesAsync(string searchText, CancellationToken cancellationToken = default);

    public Task<EmployeeProfileDto> GetEmployeeProfileAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>The HR Dashboard's KPI card numbers.</summary>
    public Task<HrDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}
