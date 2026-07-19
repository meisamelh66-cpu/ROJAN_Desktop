namespace Rojan.Desktop.Domain.HR;

/// <summary>
/// Repository abstraction for the HR vertical slice - covers all nine
/// related aggregate types (Employee, EmployeeProfile, Shift,
/// ShiftAssignment, Attendance, LeaveRequest, CommissionRule,
/// CommissionTransaction, PayrollSummary) in one interface, the widest
/// single repository interface in this app - same "one repository per
/// slice" convention <c>Inventory.IInventoryRepository</c> (six aggregate
/// types) and <c>Accounting.IAccountingRepository</c> (five aggregate
/// types) already established. Every "get many" method returns the full
/// set; Application filters/composes, consistent with the "return the
/// read-set, compose in Application" convention every prior module
/// follows. Deliberately "dumb" - commission math
/// (<see cref="CommissionCalculator"/>) and payroll math
/// (<see cref="PayrollCalculator"/>) are Application's job, not this
/// repository's.
/// </summary>
public interface IHrRepository
{
    public Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default);

    public Task<Employee?> GetEmployeeByIdAsync(string employeeId, CancellationToken cancellationToken = default);

    public Task<Employee> CreateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);

    public Task<Employee> UpdateEmployeeStatusAsync(string employeeId, EmployeeStatus status, CancellationToken cancellationToken = default);

    public Task<Employee> UpdateEmployeeDepartmentAsync(string employeeId, Department department, CancellationToken cancellationToken = default);

    public Task<EmployeeProfile?> GetEmployeeProfileAsync(string employeeId, CancellationToken cancellationToken = default);

    public Task<EmployeeProfile> UpsertEmployeeProfileAsync(EmployeeProfile profile, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken cancellationToken = default);

    public Task<Shift> CreateShiftAsync(Shift shift, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ShiftAssignment>> GetShiftAssignmentsAsync(CancellationToken cancellationToken = default);

    public Task<ShiftAssignment> CreateShiftAssignmentAsync(ShiftAssignment assignment, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Attendance>> GetAttendanceAsync(CancellationToken cancellationToken = default);

    public Task<Attendance> RecordAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default);

    public Task<Attendance> UpdateAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<LeaveRequest>> GetLeaveRequestsAsync(CancellationToken cancellationToken = default);

    public Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);

    public Task<LeaveRequest> UpdateLeaveRequestStatusAsync(string leaveRequestId, LeaveStatus status, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CommissionRule>> GetCommissionRulesAsync(CancellationToken cancellationToken = default);

    public Task<CommissionRule> CreateCommissionRuleAsync(CommissionRule rule, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CommissionTransaction>> GetCommissionTransactionsAsync(CancellationToken cancellationToken = default);

    public Task<CommissionTransaction> CreateCommissionTransactionAsync(CommissionTransaction transaction, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PayrollSummary>> GetPayrollSummariesAsync(CancellationToken cancellationToken = default);

    public Task<PayrollSummary> CreatePayrollSummaryAsync(PayrollSummary summary, CancellationToken cancellationToken = default);
}
