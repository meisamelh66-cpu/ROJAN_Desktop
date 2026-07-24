using Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Domain.Tests.Bookings;

public sealed class BookingRulesTests
{
    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Pending, BookingStatus.InProgress, true)]
    [InlineData(BookingStatus.Pending, BookingStatus.Cancelled, true)]
    [InlineData(BookingStatus.Pending, BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Pending, BookingStatus.NoShow, false)]
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

    private static readonly DateTimeOffset Anchor = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TimeRangesOverlap_IdenticalStartAndDuration_ReturnsTrue()
    {
        Assert.True(BookingRules.TimeRangesOverlap(Anchor, 60, Anchor, 60));
    }

    [Fact]
    public void TimeRangesOverlap_SecondStartsInsideFirst_ReturnsTrue()
    {
        Assert.True(BookingRules.TimeRangesOverlap(Anchor, 60, Anchor.AddMinutes(30), 60));
    }

    [Fact]
    public void TimeRangesOverlap_FirstStartsInsideSecond_ReturnsTrue()
    {
        Assert.True(BookingRules.TimeRangesOverlap(Anchor.AddMinutes(30), 60, Anchor, 60));
    }

    [Fact]
    public void TimeRangesOverlap_SecondEntirelyWithinFirst_ReturnsTrue()
    {
        Assert.True(BookingRules.TimeRangesOverlap(Anchor, 120, Anchor.AddMinutes(30), 30));
    }

    [Fact]
    public void TimeRangesOverlap_SecondStartsExactlyWhenFirstEnds_ReturnsFalse()
    {
        // Back-to-back, non-overlapping appointments - a 9:00-10:00 slot and a 10:00-11:00 slot
        // for the same specialist are adjacent, not conflicting.
        Assert.False(BookingRules.TimeRangesOverlap(Anchor, 60, Anchor.AddMinutes(60), 60));
    }

    [Fact]
    public void TimeRangesOverlap_CompletelyDisjointRanges_ReturnsFalse()
    {
        Assert.False(BookingRules.TimeRangesOverlap(Anchor, 60, Anchor.AddHours(3), 60));
    }

    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.InProgress, true)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    [InlineData(BookingStatus.NoShow, false)]
    public void IsActive_VariousStatuses_MatchesExpectedLifecycle(BookingStatus status, bool expected)
    {
        Assert.Equal(expected, BookingRules.IsActive(status));
    }
}
