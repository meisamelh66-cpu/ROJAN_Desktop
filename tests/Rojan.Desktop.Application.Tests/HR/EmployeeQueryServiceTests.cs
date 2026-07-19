using Rojan.Desktop.Application.HR;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

public sealed class EmployeeQueryServiceTests
{
    private static DomainHr.Employee MakeEmployee(string id, string fullName, DomainHr.EmployeeStatus status = DomainHr.EmployeeStatus.Active) =>
        new(id, string.Empty, fullName, $"{id}@rojan.example", "+1 555", DomainHr.EmployeeRole.Stylist, DomainHr.Department.Hair, DomainHr.EmploymentType.FullTime, status, new DateOnly(2022, 1, 1), 2500m);

    private static EmployeeQueryService MakeSut(StubHrRepository? repository = null) => new(repository ?? new StubHrRepository());

    [Fact]
    public async Task GetEmployeesAsync_ReturnsMappedEmployees()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        var sut = MakeSut(repository);

        var result = await sut.GetEmployeesAsync();

        var employee = Assert.Single(result);
        Assert.Equal("Jordan Lee", employee.FullName);
    }

    [Fact]
    public async Task SearchEmployeesAsync_MatchesFullName_ReturnsOnlyMatchingEmployees()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        repository.Employees.Add(MakeEmployee("employee-2", "Priya Nair"));
        var sut = MakeSut(repository);

        var result = await sut.SearchEmployeesAsync("priya");

        var employee = Assert.Single(result);
        Assert.Equal("employee-2", employee.Id);
    }

    [Fact]
    public async Task GetEmployeeProfileAsync_UnknownEmployee_ThrowsInvalidOperationException()
    {
        var sut = MakeSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetEmployeeProfileAsync("no-such-employee"));
    }

    [Fact]
    public async Task GetEmployeeProfileAsync_KnownEmployee_AggregatesAttendanceShiftsLeaveAndCommissions()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        var today = DateOnly.FromDateTime(DateTime.Now);
        repository.Attendance.Add(new DomainHr.Attendance("attendance-1", "employee-1", "Jordan Lee", today, new TimeSpan(9, 0, 0), null, DomainHr.AttendanceStatus.Present, string.Empty));
        repository.ShiftAssignments.Add(new DomainHr.ShiftAssignment("assignment-1", "shift-1", "employee-1", "Jordan Lee", today.AddDays(1)));
        repository.LeaveRequests.Add(new DomainHr.LeaveRequest("leave-1", "employee-1", "Jordan Lee", today, today.AddDays(2), "Vacation", DomainHr.LeaveStatus.Pending, DateTimeOffset.Now));
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut", 65m, 6.5m, DateTimeOffset.Now));
        var sut = MakeSut(repository);

        var profile = await sut.GetEmployeeProfileAsync("employee-1");

        Assert.Equal("Jordan Lee", profile.Employee.FullName);
        Assert.Single(profile.RecentAttendance);
        Assert.Single(profile.UpcomingShifts);
        Assert.Single(profile.LeaveRequests);
        Assert.Single(profile.RecentCommissions);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ComputesCountsAndTotals()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        repository.Employees.Add(MakeEmployee("employee-2", "Priya Nair"));
        repository.Employees.Add(MakeEmployee("employee-3", "Isabella Cruz", DomainHr.EmployeeStatus.OnLeave));

        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.Date);
        repository.Attendance.Add(new DomainHr.Attendance("attendance-1", "employee-1", "Jordan Lee", today, new TimeSpan(9, 0, 0), null, DomainHr.AttendanceStatus.Present, string.Empty));
        repository.Attendance.Add(new DomainHr.Attendance("attendance-2", "employee-2", "Priya Nair", today, new TimeSpan(9, 20, 0), null, DomainHr.AttendanceStatus.Late, string.Empty));

        repository.PayrollSummaries.Add(new DomainHr.PayrollSummary("payroll-1", "employee-1", "Jordan Lee", now.Month, now.Year, 3200m, 450m, 100m, 50m, 3700m, now));
        repository.CommissionTransactions.Add(new DomainHr.CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut", 65m, 6.5m, now));

        var sut = MakeSut(repository);

        var summary = await sut.GetDashboardSummaryAsync();

        Assert.Equal(3, summary.EmployeeCount);
        Assert.Equal(1, summary.PresentToday);
        Assert.Equal(1, summary.LateToday);
        Assert.Equal(1, summary.OnLeaveCount);
        Assert.Equal(3700m, summary.PayrollThisMonth);
        Assert.Equal(6.5m, summary.CommissionThisMonth);
        Assert.Equal(100m, summary.AverageAttendancePercent);
    }
}
