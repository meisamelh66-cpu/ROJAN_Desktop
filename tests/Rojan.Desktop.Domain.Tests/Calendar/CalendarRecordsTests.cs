using Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Domain.Tests.Calendar;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class CalendarRecordsTests
{
    [Fact]
    public void WorkingSchedule_SameValues_AreEqual()
    {
        var first = new WorkingSchedule("schedule-1", "specialist-1", "Jordan Lee", DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));
        var second = new WorkingSchedule("schedule-1", "specialist-1", "Jordan Lee", DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));

        Assert.Equal(first, second);
    }

    [Fact]
    public void TimeSlot_DifferentEnd_AreNotEqual()
    {
        var start = DateTimeOffset.UnixEpoch;
        var first = new TimeSlot(start, start.AddMinutes(30));
        var second = new TimeSlot(start, start.AddMinutes(60));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void AvailabilitySlot_DifferentStatus_AreNotEqual()
    {
        var start = DateTimeOffset.UnixEpoch;
        var first = new AvailabilitySlot("specialist-1", "Jordan Lee", start, start.AddMinutes(30), AvailabilityStatus.Available);
        var second = new AvailabilitySlot("specialist-1", "Jordan Lee", start, start.AddMinutes(30), AvailabilityStatus.Booked);

        Assert.NotEqual(first, second);
    }
}
