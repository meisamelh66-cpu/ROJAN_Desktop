using Rojan.Desktop.Domain.HR;
using Rojan.Desktop.Infrastructure.HR;

namespace Rojan.Desktop.Infrastructure.Tests.HR;

/// <summary>Smoke + behavioral coverage - same reasoning as Accounting.FakeAccountingRepositoryTests, covering all nine seeded HR aggregate types.</summary>
public sealed class FakeHrRepositoryTests
{
    [Fact]
    public async Task GetEmployeesAsync_ReturnsTwentySeededEmployees()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetEmployeesAsync();

        Assert.Equal(20, result.Count);
    }

    [Fact]
    public async Task GetEmployeesAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeHrRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetEmployeesAsync(cts.Token));
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_KnownId_ReturnsMatchingEmployee()
    {
        var sut = new FakeHrRepository();

        var employee = await sut.GetEmployeeByIdAsync("employee-1");

        Assert.NotNull(employee);
        Assert.Equal("Jordan Lee", employee.FullName);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = new FakeHrRepository();

        var employee = await sut.GetEmployeeByIdAsync("no-such-employee");

        Assert.Null(employee);
    }

    [Fact]
    public async Task CreateEmployeeAsync_NewEmployee_BecomesVisibleViaGetEmployeesAsync()
    {
        var sut = new FakeHrRepository();
        var employee = new Employee("employee-new", string.Empty, "Test Person", "test@rojan.example", "+1 555", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2026, 1, 1), 2500m);

        await sut.CreateEmployeeAsync(employee);
        var employees = await sut.GetEmployeesAsync();

        Assert.Contains(employees, e => e.Id == "employee-new");
    }

    [Fact]
    public async Task UpdateEmployeeStatusAsync_ExistingEmployee_ChangesStatus()
    {
        var sut = new FakeHrRepository();

        var updated = await sut.UpdateEmployeeStatusAsync("employee-2", EmployeeStatus.Suspended);
        var reloaded = await sut.GetEmployeeByIdAsync("employee-2");

        Assert.Equal(EmployeeStatus.Suspended, updated.Status);
        Assert.Equal(EmployeeStatus.Suspended, reloaded!.Status);
    }

    [Fact]
    public async Task UpdateEmployeeStatusAsync_UnknownEmployee_ThrowsInvalidOperationException()
    {
        var sut = new FakeHrRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateEmployeeStatusAsync("no-such-employee", EmployeeStatus.Active));
    }

    [Fact]
    public async Task UpdateEmployeeDepartmentAsync_ExistingEmployee_ChangesDepartment()
    {
        var sut = new FakeHrRepository();

        var updated = await sut.UpdateEmployeeDepartmentAsync("employee-6", Department.Management);

        Assert.Equal(Department.Management, updated.Department);
    }

    [Fact]
    public async Task GetEmployeeProfileAsync_KnownEmployee_ReturnsProfile()
    {
        var sut = new FakeHrRepository();

        var profile = await sut.GetEmployeeProfileAsync("employee-1");

        Assert.NotNull(profile);
        Assert.Equal("employee-1", profile.EmployeeId);
    }

    [Fact]
    public async Task GetEmployeeProfileAsync_EmployeeWithNoProfile_ReturnsNull()
    {
        var sut = new FakeHrRepository();

        var profile = await sut.GetEmployeeProfileAsync("employee-20");

        Assert.Null(profile);
    }

    [Fact]
    public async Task UpsertEmployeeProfileAsync_NewProfile_BecomesVisible()
    {
        var sut = new FakeHrRepository();
        var profile = new EmployeeProfile("profile-new", "employee-20", "Bio", "Skills", "Contact", "+1 000");

        await sut.UpsertEmployeeProfileAsync(profile);
        var reloaded = await sut.GetEmployeeProfileAsync("employee-20");

        Assert.NotNull(reloaded);
        Assert.Equal("Bio", reloaded.Bio);
    }

    [Fact]
    public async Task GetShiftsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetShiftsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateShiftAsync_NewShift_BecomesVisibleViaGetShiftsAsync()
    {
        var sut = new FakeHrRepository();
        var shift = new Shift("shift-new", "Evening - Hair", Department.Hair, new TimeSpan(12, 0, 0), new TimeSpan(20, 0, 0));

        await sut.CreateShiftAsync(shift);
        var shifts = await sut.GetShiftsAsync();

        Assert.Contains(shifts, s => s.Id == "shift-new");
    }

    [Fact]
    public async Task GetShiftAssignmentsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetShiftAssignmentsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateShiftAssignmentAsync_NewAssignment_BecomesVisible()
    {
        var sut = new FakeHrRepository();
        var assignment = new ShiftAssignment("assignment-new", "shift-1", "employee-1", "Jordan Lee", new DateOnly(2026, 8, 1));

        await sut.CreateShiftAssignmentAsync(assignment);
        var assignments = await sut.GetShiftAssignmentsAsync();

        Assert.Contains(assignments, a => a.Id == "assignment-new");
    }

    [Fact]
    public async Task GetAttendanceAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetAttendanceAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task RecordAttendanceAsync_NewRecord_BecomesVisibleViaGetAttendanceAsync()
    {
        var sut = new FakeHrRepository();
        var attendance = new Attendance("attendance-new", "employee-1", "Jordan Lee", new DateOnly(2026, 8, 1), new TimeSpan(9, 0, 0), null, AttendanceStatus.Present, string.Empty);

        await sut.RecordAttendanceAsync(attendance);
        var records = await sut.GetAttendanceAsync();

        Assert.Contains(records, a => a.Id == "attendance-new");
    }

    [Fact]
    public async Task UpdateAttendanceAsync_ExistingRecord_ReplacesIt()
    {
        var sut = new FakeHrRepository();
        var records = await sut.GetAttendanceAsync();
        var existing = records[0];
        var corrected = existing with { Status = AttendanceStatus.Absent };

        var updated = await sut.UpdateAttendanceAsync(corrected);

        Assert.Equal(AttendanceStatus.Absent, updated.Status);
    }

    [Fact]
    public async Task UpdateAttendanceAsync_UnknownRecord_ThrowsInvalidOperationException()
    {
        var sut = new FakeHrRepository();
        var fake = new Attendance("no-such-record", "employee-1", "Jordan Lee", new DateOnly(2026, 8, 1), null, null, AttendanceStatus.Absent, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateAttendanceAsync(fake));
    }

    [Fact]
    public async Task GetLeaveRequestsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetLeaveRequestsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateLeaveRequestAsync_NewRequest_BecomesVisible()
    {
        var sut = new FakeHrRepository();
        var leaveRequest = new LeaveRequest("leave-new", "employee-1", "Jordan Lee", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "Personal", LeaveStatus.Pending, DateTimeOffset.Now);

        await sut.CreateLeaveRequestAsync(leaveRequest);
        var requests = await sut.GetLeaveRequestsAsync();

        Assert.Contains(requests, l => l.Id == "leave-new");
    }

    [Fact]
    public async Task UpdateLeaveRequestStatusAsync_ExistingRequest_ChangesStatus()
    {
        var sut = new FakeHrRepository();

        var updated = await sut.UpdateLeaveRequestStatusAsync("leave-2", LeaveStatus.Approved);

        Assert.Equal(LeaveStatus.Approved, updated.Status);
    }

    [Fact]
    public async Task UpdateLeaveRequestStatusAsync_UnknownRequest_ThrowsInvalidOperationException()
    {
        var sut = new FakeHrRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateLeaveRequestStatusAsync("no-such-leave", LeaveStatus.Approved));
    }

    [Fact]
    public async Task GetCommissionRulesAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetCommissionRulesAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateCommissionRuleAsync_NewRule_BecomesVisible()
    {
        var sut = new FakeHrRepository();
        var rule = new CommissionRule("rule-new", "employee-6", "Amelia Ross", CommissionType.Percentage, 0.05m, string.Empty);

        await sut.CreateCommissionRuleAsync(rule);
        var rules = await sut.GetCommissionRulesAsync();

        Assert.Contains(rules, r => r.Id == "rule-new");
    }

    [Fact]
    public async Task GetCommissionTransactionsAsync_ReturnsFourSeededTransactions()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetCommissionTransactionsAsync();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task CreateCommissionTransactionAsync_NewTransaction_BecomesVisible()
    {
        var sut = new FakeHrRepository();
        var transaction = new CommissionTransaction("commission-new", "employee-2", "Priya Nair", "invoice-8", "Corporate Group Styling", 518.40m, 62.21m, DateTimeOffset.Now);

        await sut.CreateCommissionTransactionAsync(transaction);
        var transactions = await sut.GetCommissionTransactionsAsync();

        Assert.Contains(transactions, t => t.Id == "commission-new");
    }

    [Fact]
    public async Task GetPayrollSummariesAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeHrRepository();

        var result = await sut.GetPayrollSummariesAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreatePayrollSummaryAsync_NewSummary_BecomesVisible()
    {
        var sut = new FakeHrRepository();
        var summary = new PayrollSummary("payroll-new", "employee-2", "Priya Nair", 7, 2026, 2900m, 100m, 0m, 0m, 3000m, DateTimeOffset.Now);

        await sut.CreatePayrollSummaryAsync(summary);
        var summaries = await sut.GetPayrollSummariesAsync();

        Assert.Contains(summaries, s => s.Id == "payroll-new");
    }
}
