using Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Domain.Tests.HR;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class HrRecordsTests
{
    [Fact]
    public void Employee_SameValues_AreEqual()
    {
        var first = new Employee("employee-1", "specialist-1", "Jordan Lee", "jordan.lee@rojan.example", "+1 555", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2021, 3, 10), 3200m);
        var second = new Employee("employee-1", "specialist-1", "Jordan Lee", "jordan.lee@rojan.example", "+1 555", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2021, 3, 10), 3200m);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Employee_DifferentStatus_AreNotEqual()
    {
        var first = new Employee("employee-1", "specialist-1", "Jordan Lee", "jordan.lee@rojan.example", "+1 555", EmployeeRole.Colorist, Department.Hair, EmploymentType.FullTime, EmployeeStatus.Active, new DateOnly(2021, 3, 10), 3200m);
        var second = first with { Status = EmployeeStatus.Suspended };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void EmployeeProfile_DifferentBio_AreNotEqual()
    {
        var first = new EmployeeProfile("profile-1", "employee-1", "Bio A", "Skill A", "Contact A", "+1 000");
        var second = first with { Bio = "Bio B" };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Shift_SameValues_AreEqual()
    {
        var first = new Shift("shift-1", "Morning - Hair", Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));
        var second = new Shift("shift-1", "Morning - Hair", Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));

        Assert.Equal(first, second);
    }

    [Fact]
    public void ShiftAssignment_DifferentDate_AreNotEqual()
    {
        var first = new ShiftAssignment("assignment-1", "shift-1", "employee-1", "Jordan Lee", new DateOnly(2026, 7, 19));
        var second = first with { AssignedDate = new DateOnly(2026, 7, 20) };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Attendance_DifferentStatus_AreNotEqual()
    {
        var first = new Attendance("attendance-1", "employee-1", "Jordan Lee", new DateOnly(2026, 7, 19), new TimeSpan(9, 0, 0), null, AttendanceStatus.Present, string.Empty);
        var second = first with { Status = AttendanceStatus.Late };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void LeaveRequest_DifferentStatus_AreNotEqual()
    {
        var first = new LeaveRequest("leave-1", "employee-1", "Jordan Lee", new DateOnly(2026, 7, 19), new DateOnly(2026, 7, 21), "Vacation", LeaveStatus.Pending, DateTimeOffset.UnixEpoch);
        var second = first with { Status = LeaveStatus.Approved };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CommissionRule_DifferentValue_AreNotEqual()
    {
        var first = new CommissionRule("rule-1", "employee-1", "Jordan Lee", CommissionType.Percentage, 0.10m, "Standard");
        var second = first with { Value = 0.15m };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CommissionTransaction_SameValues_AreEqual()
    {
        var earnedAt = DateTimeOffset.UnixEpoch;
        var first = new CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut & Style", 65m, 6.50m, earnedAt);
        var second = new CommissionTransaction("commission-1", "employee-1", "Jordan Lee", "invoice-1", "Haircut & Style", 65m, 6.50m, earnedAt);

        Assert.Equal(first, second);
    }

    [Fact]
    public void PayrollSummary_DifferentNetSalary_AreNotEqual()
    {
        var first = new PayrollSummary("payroll-1", "employee-1", "Jordan Lee", 6, 2026, 3200m, 450m, 100m, 50m, 3700m, DateTimeOffset.UnixEpoch);
        var second = first with { NetSalary = 3800m };

        Assert.NotEqual(first, second);
    }
}
