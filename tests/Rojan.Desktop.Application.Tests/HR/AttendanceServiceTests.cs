using Rojan.Desktop.Application.HR;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

public sealed class AttendanceServiceTests
{
    private static DomainHr.Employee MakeEmployee(string id, string fullName) =>
        new(id, string.Empty, fullName, $"{id}@rojan.example", "+1 555", DomainHr.EmployeeRole.Stylist, DomainHr.Department.Hair, DomainHr.EmploymentType.FullTime, DomainHr.EmployeeStatus.Active, new DateOnly(2022, 1, 1), 2500m);

    [Fact]
    public async Task GetTodayAttendanceAsync_ReturnsOnlyTodaysRecords()
    {
        var repository = new StubHrRepository();
        var today = DateOnly.FromDateTime(DateTime.Now);
        repository.Attendance.Add(new DomainHr.Attendance("attendance-1", "employee-1", "Jordan Lee", today, new TimeSpan(9, 0, 0), null, DomainHr.AttendanceStatus.Present, string.Empty));
        repository.Attendance.Add(new DomainHr.Attendance("attendance-2", "employee-1", "Jordan Lee", today.AddDays(-1), new TimeSpan(9, 0, 0), null, DomainHr.AttendanceStatus.Present, string.Empty));
        var sut = new AttendanceQueryService(repository);

        var result = await sut.GetTodayAttendanceAsync();

        var record = Assert.Single(result);
        Assert.Equal("attendance-1", record.Id);
    }

    [Fact]
    public async Task RecordAttendanceAsync_CheckOutBeforeCheckIn_ThrowsArgumentException()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        var sut = new AttendanceCommandService(repository);
        var request = new RecordAttendanceRequest("employee-1", DateOnly.FromDateTime(DateTime.Now), new TimeSpan(17, 0, 0), new TimeSpan(9, 0, 0), null, string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.RecordAttendanceAsync(request));
    }

    [Fact]
    public async Task RecordAttendanceAsync_NoCheckIn_DefaultsToAbsent()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        var sut = new AttendanceCommandService(repository);
        var request = new RecordAttendanceRequest("employee-1", DateOnly.FromDateTime(DateTime.Now), null, null, null, "Called in sick.");

        var result = await sut.RecordAttendanceAsync(request);

        Assert.Equal(AttendanceStatus.Absent, result.Status);
    }

    [Fact]
    public async Task RecordAttendanceAsync_CheckInWithinShiftGrace_DerivesPresent()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        var today = DateOnly.FromDateTime(DateTime.Now);
        repository.Shifts.Add(new DomainHr.Shift("shift-1", "Morning", DomainHr.Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
        repository.ShiftAssignments.Add(new DomainHr.ShiftAssignment("assignment-1", "shift-1", "employee-1", "Jordan Lee", today));
        var sut = new AttendanceCommandService(repository);
        var request = new RecordAttendanceRequest("employee-1", today, new TimeSpan(9, 5, 0), null, null, string.Empty);

        var result = await sut.RecordAttendanceAsync(request);

        Assert.Equal(AttendanceStatus.Present, result.Status);
    }

    [Fact]
    public async Task RecordAttendanceAsync_CheckInPastShiftGrace_DerivesLate()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        var today = DateOnly.FromDateTime(DateTime.Now);
        repository.Shifts.Add(new DomainHr.Shift("shift-1", "Morning", DomainHr.Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
        repository.ShiftAssignments.Add(new DomainHr.ShiftAssignment("assignment-1", "shift-1", "employee-1", "Jordan Lee", today));
        var sut = new AttendanceCommandService(repository);
        var request = new RecordAttendanceRequest("employee-1", today, new TimeSpan(9, 25, 0), null, null, string.Empty);

        var result = await sut.RecordAttendanceAsync(request);

        Assert.Equal(AttendanceStatus.Late, result.Status);
    }

    [Fact]
    public async Task CorrectAttendanceAsync_UnknownRecord_ThrowsInvalidOperationException()
    {
        var repository = new StubHrRepository();
        var sut = new AttendanceCommandService(repository);
        var request = new CorrectAttendanceRequest("no-such-record", new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), AttendanceStatus.Present, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CorrectAttendanceAsync(request));
    }

    [Fact]
    public async Task CorrectAttendanceAsync_ExistingRecord_UpdatesStatusAndTimes()
    {
        var repository = new StubHrRepository();
        var today = DateOnly.FromDateTime(DateTime.Now);
        repository.Attendance.Add(new DomainHr.Attendance("attendance-1", "employee-1", "Jordan Lee", today, new TimeSpan(9, 20, 0), null, DomainHr.AttendanceStatus.Late, string.Empty));
        var sut = new AttendanceCommandService(repository);
        var request = new CorrectAttendanceRequest("attendance-1", new TimeSpan(8, 55, 0), new TimeSpan(17, 0, 0), AttendanceStatus.Present, "Corrected - badge scan delay.");

        var result = await sut.CorrectAttendanceAsync(request);

        Assert.Equal(AttendanceStatus.Present, result.Status);
        Assert.Equal(new TimeSpan(8, 55, 0), result.CheckInTime);
    }

    [Fact]
    public async Task RequestLeaveAsync_ValidRequest_CreatesPendingLeaveRequest()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", "Jordan Lee"));
        var sut = new AttendanceCommandService(repository);
        var request = new CreateLeaveRequestRequest("employee-1", DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(2)), "Vacation");

        var result = await sut.RequestLeaveAsync(request);

        Assert.Equal(LeaveStatus.Pending, result.Status);
        Assert.Single(repository.LeaveRequests);
    }

    [Fact]
    public async Task ApproveLeaveAsync_ExistingRequest_SetsStatusToApproved()
    {
        var repository = new StubHrRepository();
        repository.LeaveRequests.Add(new DomainHr.LeaveRequest("leave-1", "employee-1", "Jordan Lee", DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(2)), "Vacation", DomainHr.LeaveStatus.Pending, DateTimeOffset.Now));
        var sut = new AttendanceCommandService(repository);

        var result = await sut.ApproveLeaveAsync("leave-1");

        Assert.Equal(LeaveStatus.Approved, result.Status);
    }

    [Fact]
    public async Task RejectLeaveAsync_ExistingRequest_SetsStatusToRejected()
    {
        var repository = new StubHrRepository();
        repository.LeaveRequests.Add(new DomainHr.LeaveRequest("leave-1", "employee-1", "Jordan Lee", DateOnly.FromDateTime(DateTime.Now), DateOnly.FromDateTime(DateTime.Now.AddDays(2)), "Vacation", DomainHr.LeaveStatus.Pending, DateTimeOffset.Now));
        var sut = new AttendanceCommandService(repository);

        var result = await sut.RejectLeaveAsync("leave-1");

        Assert.Equal(LeaveStatus.Rejected, result.Status);
    }
}
