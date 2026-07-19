using Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

/// <summary>In-memory, mutable <see cref="IHrRepository"/> test double - same reasoning as Accounting.StubAccountingRepository, covering all nine HR aggregate types.</summary>
internal sealed class StubHrRepository : IHrRepository
{
    public List<Employee> Employees { get; } = [];

    public List<EmployeeProfile> EmployeeProfiles { get; } = [];

    public List<Shift> Shifts { get; } = [];

    public List<ShiftAssignment> ShiftAssignments { get; } = [];

    public List<Attendance> Attendance { get; } = [];

    public List<LeaveRequest> LeaveRequests { get; } = [];

    public List<CommissionRule> CommissionRules { get; } = [];

    public List<CommissionTransaction> CommissionTransactions { get; } = [];

    public List<PayrollSummary> PayrollSummaries { get; } = [];

    public Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Employee>>(Employees.ToList());

    public Task<Employee?> GetEmployeeByIdAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Employees.FirstOrDefault(employee => employee.Id == employeeId));

    public Task<Employee> CreateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        Employees.Add(employee);
        return Task.FromResult(employee);
    }

    public Task<Employee> UpdateEmployeeStatusAsync(string employeeId, EmployeeStatus status, CancellationToken cancellationToken = default)
    {
        var index = Employees.FindIndex(employee => employee.Id == employeeId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Employee '{employeeId}' was not found.");
        }

        var updated = Employees[index] with { Status = status };
        Employees[index] = updated;
        return Task.FromResult(updated);
    }

    public Task<Employee> UpdateEmployeeDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default)
    {
        var index = Employees.FindIndex(employee => employee.Id == employeeId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Employee '{employeeId}' was not found.");
        }

        var updated = Employees[index] with { Department = department };
        Employees[index] = updated;
        return Task.FromResult(updated);
    }

    public Task<EmployeeProfile?> GetEmployeeProfileAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(EmployeeProfiles.FirstOrDefault(profile => profile.EmployeeId == employeeId));

    public Task<EmployeeProfile> UpsertEmployeeProfileAsync(EmployeeProfile profile, CancellationToken cancellationToken = default)
    {
        var index = EmployeeProfiles.FindIndex(existing => existing.EmployeeId == profile.EmployeeId);
        if (index < 0)
        {
            EmployeeProfiles.Add(profile);
        }
        else
        {
            EmployeeProfiles[index] = profile;
        }

        return Task.FromResult(profile);
    }

    public Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Shift>>(Shifts.ToList());

    public Task<Shift> CreateShiftAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        Shifts.Add(shift);
        return Task.FromResult(shift);
    }

    public Task<IReadOnlyList<ShiftAssignment>> GetShiftAssignmentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShiftAssignment>>(ShiftAssignments.ToList());

    public Task<ShiftAssignment> CreateShiftAssignmentAsync(ShiftAssignment assignment, CancellationToken cancellationToken = default)
    {
        ShiftAssignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    public Task<IReadOnlyList<Attendance>> GetAttendanceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Attendance>>(Attendance.ToList());

    public Task<Attendance> RecordAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        Attendance.Add(attendance);
        return Task.FromResult(attendance);
    }

    public Task<Attendance> UpdateAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        var index = Attendance.FindIndex(existing => existing.Id == attendance.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Attendance record '{attendance.Id}' was not found.");
        }

        Attendance[index] = attendance;
        return Task.FromResult(attendance);
    }

    public Task<IReadOnlyList<LeaveRequest>> GetLeaveRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LeaveRequest>>(LeaveRequests.ToList());

    public Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        LeaveRequests.Add(leaveRequest);
        return Task.FromResult(leaveRequest);
    }

    public Task<LeaveRequest> UpdateLeaveRequestStatusAsync(string leaveRequestId, LeaveStatus status, CancellationToken cancellationToken = default)
    {
        var index = LeaveRequests.FindIndex(existing => existing.Id == leaveRequestId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Leave request '{leaveRequestId}' was not found.");
        }

        var updated = LeaveRequests[index] with { Status = status };
        LeaveRequests[index] = updated;
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<CommissionRule>> GetCommissionRulesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommissionRule>>(CommissionRules.ToList());

    public Task<CommissionRule> CreateCommissionRuleAsync(CommissionRule rule, CancellationToken cancellationToken = default)
    {
        CommissionRules.Add(rule);
        return Task.FromResult(rule);
    }

    public Task<IReadOnlyList<CommissionTransaction>> GetCommissionTransactionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommissionTransaction>>(CommissionTransactions.ToList());

    public Task<CommissionTransaction> CreateCommissionTransactionAsync(CommissionTransaction transaction, CancellationToken cancellationToken = default)
    {
        CommissionTransactions.Add(transaction);
        return Task.FromResult(transaction);
    }

    public Task<IReadOnlyList<PayrollSummary>> GetPayrollSummariesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PayrollSummary>>(PayrollSummaries.ToList());

    public Task<PayrollSummary> CreatePayrollSummaryAsync(PayrollSummary summary, CancellationToken cancellationToken = default)
    {
        PayrollSummaries.Add(summary);
        return Task.FromResult(summary);
    }
}
