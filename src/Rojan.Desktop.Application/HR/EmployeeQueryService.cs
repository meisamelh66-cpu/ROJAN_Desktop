using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.HR;

/// <summary>Default <see cref="IEmployeeQueryService"/> implementation.</summary>
public sealed class EmployeeQueryService : IEmployeeQueryService
{
    private readonly DomainHr.IHrRepository _repository;

    public EmployeeQueryService(DomainHr.IHrRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _repository.GetEmployeesAsync(cancellationToken).ConfigureAwait(true);
        return employees.Select(HrMapper.MapEmployee).ToList();
    }

    public async Task<IReadOnlyList<EmployeeDto>> SearchEmployeesAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var employees = await _repository.GetEmployeesAsync(cancellationToken).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return employees.Select(HrMapper.MapEmployee).ToList();
        }

        return employees
            .Where(employee =>
                employee.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                employee.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                employee.Role.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                employee.Department.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(HrMapper.MapEmployee)
            .ToList();
    }

    public async Task<EmployeeProfileDto> GetEmployeeProfileAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetEmployeeByIdAsync(employeeId, cancellationToken).ConfigureAwait(true);
        if (employee is null)
        {
            throw new InvalidOperationException($"Employee '{employeeId}' was not found.");
        }

        var detail = await _repository.GetEmployeeProfileAsync(employeeId, cancellationToken).ConfigureAwait(true);
        var attendance = await _repository.GetAttendanceAsync(cancellationToken).ConfigureAwait(true);
        var shiftAssignments = await _repository.GetShiftAssignmentsAsync(cancellationToken).ConfigureAwait(true);
        var leaveRequests = await _repository.GetLeaveRequestsAsync(cancellationToken).ConfigureAwait(true);
        var commissions = await _repository.GetCommissionTransactionsAsync(cancellationToken).ConfigureAwait(true);

        var today = DateOnly.FromDateTime(DateTime.Now);

        return new EmployeeProfileDto(
            HrMapper.MapEmployee(employee),
            detail is null ? null : HrMapper.MapDetail(detail),
            attendance.Where(a => a.EmployeeId == employeeId).OrderByDescending(a => a.Date).Take(10).Select(HrMapper.MapAttendance).ToList(),
            shiftAssignments.Where(a => a.EmployeeId == employeeId && a.AssignedDate >= today).OrderBy(a => a.AssignedDate).Select(HrMapper.MapShiftAssignment).ToList(),
            leaveRequests.Where(l => l.EmployeeId == employeeId).OrderByDescending(l => l.RequestedAt).Select(HrMapper.MapLeaveRequest).ToList(),
            commissions.Where(c => c.EmployeeId == employeeId).OrderByDescending(c => c.EarnedAt).Take(10).Select(HrMapper.MapCommissionTransaction).ToList());
    }

    public async Task<HrDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _repository.GetEmployeesAsync(cancellationToken).ConfigureAwait(true);
        var attendance = await _repository.GetAttendanceAsync(cancellationToken).ConfigureAwait(true);
        var payrollSummaries = await _repository.GetPayrollSummariesAsync(cancellationToken).ConfigureAwait(true);
        var commissionTransactions = await _repository.GetCommissionTransactionsAsync(cancellationToken).ConfigureAwait(true);

        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.DateTime);
        var todayAttendance = attendance.Where(a => a.Date == today).ToList();

        var presentToday = todayAttendance.Count(a => a.Status == DomainHr.AttendanceStatus.Present);
        var lateToday = todayAttendance.Count(a => a.Status == DomainHr.AttendanceStatus.Late);
        var onLeaveCount = employees.Count(e => e.Status == DomainHr.EmployeeStatus.OnLeave);

        var payrollThisMonth = payrollSummaries
            .Where(p => p.Month == now.Month && p.Year == now.Year)
            .Sum(p => p.NetSalary);

        var commissionThisMonth = commissionTransactions
            .Where(c => c.EarnedAt.Month == now.Month && c.EarnedAt.Year == now.Year)
            .Sum(c => c.CommissionAmount);

        var averageAttendancePercent = attendance.Count == 0
            ? 0m
            : (decimal)attendance.Count(a => a.Status is DomainHr.AttendanceStatus.Present or DomainHr.AttendanceStatus.Late) / attendance.Count * 100m;

        return new HrDashboardSummaryDto(
            employees.Count, presentToday, lateToday, onLeaveCount,
            payrollThisMonth, commissionThisMonth, Math.Round(averageAttendancePercent, 1, MidpointRounding.AwayFromZero));
    }
}
