using Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Domain.Tests.HR;

public sealed class AttendanceRulesTests
{
    [Fact]
    public void DetermineStatus_CheckInBeforeShiftStart_ReturnsPresent()
    {
        var result = AttendanceRules.DetermineStatus(new TimeSpan(9, 0, 0), new TimeSpan(8, 55, 0), TimeSpan.FromMinutes(10));

        Assert.Equal(AttendanceStatus.Present, result);
    }

    [Fact]
    public void DetermineStatus_CheckInWithinGraceWindow_ReturnsPresent()
    {
        var result = AttendanceRules.DetermineStatus(new TimeSpan(9, 0, 0), new TimeSpan(9, 10, 0), TimeSpan.FromMinutes(10));

        Assert.Equal(AttendanceStatus.Present, result);
    }

    [Fact]
    public void DetermineStatus_CheckInAfterGraceWindow_ReturnsLate()
    {
        var result = AttendanceRules.DetermineStatus(new TimeSpan(9, 0, 0), new TimeSpan(9, 11, 0), TimeSpan.FromMinutes(10));

        Assert.Equal(AttendanceStatus.Late, result);
    }

    [Fact]
    public void IsValidCorrection_CheckOutAfterCheckIn_ReturnsTrue()
    {
        Assert.True(AttendanceRules.IsValidCorrection(new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
    }

    [Fact]
    public void IsValidCorrection_CheckOutBeforeCheckIn_ReturnsFalse()
    {
        Assert.False(AttendanceRules.IsValidCorrection(new TimeSpan(17, 0, 0), new TimeSpan(9, 0, 0)));
    }

    [Fact]
    public void IsValidCorrection_NoCheckOut_ReturnsTrue()
    {
        Assert.True(AttendanceRules.IsValidCorrection(new TimeSpan(9, 0, 0), null));
    }

    [Fact]
    public void IsValidCorrection_NoCheckIn_ReturnsTrue()
    {
        Assert.True(AttendanceRules.IsValidCorrection(null, new TimeSpan(17, 0, 0)));
    }
}
