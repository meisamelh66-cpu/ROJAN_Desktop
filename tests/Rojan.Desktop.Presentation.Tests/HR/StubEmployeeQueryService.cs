using Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Presentation.Tests.HR;

/// <summary>Configurable <see cref="IEmployeeQueryService"/> test double - same reasoning as Inventory.StubProductQueryService.</summary>
internal sealed class StubEmployeeQueryService : IEmployeeQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<EmployeeDto>>> _getEmployees;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<EmployeeDto>>>? _searchEmployees;
    private readonly Func<string, CancellationToken, Task<EmployeeProfileDto>>? _getProfile;
    private readonly Func<CancellationToken, Task<HrDashboardSummaryDto>>? _getDashboardSummary;

    public StubEmployeeQueryService(
        Func<CancellationToken, Task<IReadOnlyList<EmployeeDto>>> getEmployees,
        Func<string, CancellationToken, Task<IReadOnlyList<EmployeeDto>>>? searchEmployees = null,
        Func<string, CancellationToken, Task<EmployeeProfileDto>>? getProfile = null,
        Func<CancellationToken, Task<HrDashboardSummaryDto>>? getDashboardSummary = null)
    {
        _getEmployees = getEmployees;
        _searchEmployees = searchEmployees;
        _getProfile = getProfile;
        _getDashboardSummary = getDashboardSummary;
    }

    public Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default) =>
        _getEmployees(cancellationToken);

    public async Task<IReadOnlyList<EmployeeDto>> SearchEmployeesAsync(string searchText, CancellationToken cancellationToken = default)
    {
        if (_searchEmployees is not null)
        {
            return await _searchEmployees(searchText, cancellationToken).ConfigureAwait(true);
        }

        var employees = await _getEmployees(cancellationToken).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return employees;
        }

        return employees
            .Where(employee => employee.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Task<EmployeeProfileDto> GetEmployeeProfileAsync(string employeeId, CancellationToken cancellationToken = default) =>
        _getProfile?.Invoke(employeeId, cancellationToken) ?? throw new NotSupportedException();

    public Task<HrDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default) =>
        _getDashboardSummary?.Invoke(cancellationToken) ?? Task.FromResult(new HrDashboardSummaryDto(0, 0, 0, 0, 0m, 0m, 0m));
}
