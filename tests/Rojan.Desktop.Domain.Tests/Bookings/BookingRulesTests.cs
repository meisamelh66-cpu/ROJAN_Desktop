using Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Domain.Tests.Bookings;

public sealed class BookingRulesTests
{
    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Pending, BookingStatus.Cancelled, true)]
    [InlineData(BookingStatus.Pending, BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.InProgress, true)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Cancelled, true)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.NoShow, true)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Pending, false)]
    [InlineData(BookingStatus.InProgress, BookingStatus.Completed, true)]
    [InlineData(BookingStatus.InProgress, BookingStatus.Cancelled, true)]
    [InlineData(BookingStatus.InProgress, BookingStatus.NoShow, false)]
    [InlineData(BookingStatus.Completed, BookingStatus.Pending, false)]
    [InlineData(BookingStatus.Cancelled, BookingStatus.Pending, false)]
    [InlineData(BookingStatus.NoShow, BookingStatus.Pending, false)]
    [InlineData(BookingStatus.Pending, BookingStatus.Pending, false)]
    public void IsValidTransition_VariousPairs_MatchesExpectedLifecycle(BookingStatus from, BookingStatus to, bool expected)
    {
        Assert.Equal(expected, BookingRules.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-30, false)]
    [InlineData(1, true)]
    [InlineData(60, true)]
    [InlineData(480, true)]
    [InlineData(481, false)]
    public void IsValidDuration_VariousValues_MatchesExpectedRange(int durationMinutes, bool expected)
    {
        Assert.Equal(expected, BookingRules.IsValidDuration(durationMinutes));
    }
}
