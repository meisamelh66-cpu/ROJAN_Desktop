using Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Domain.Tests.Reporting;

public sealed class DateRangeRulesTests
{
    [Fact]
    public void IsValidRange_WhenStartBeforeEnd_ReturnsTrue()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);

        Assert.True(DateRangeRules.IsValidRange(start, end));
    }

    [Fact]
    public void IsValidRange_WhenStartEqualsEnd_ReturnsTrue()
    {
        var date = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.True(DateRangeRules.IsValidRange(date, date));
    }

    [Fact]
    public void IsValidRange_WhenStartAfterEnd_ReturnsFalse()
    {
        var start = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.False(DateRangeRules.IsValidRange(start, end));
    }

    [Fact]
    public void PreviousPeriod_ReturnsEqualLengthRangeImmediatelyBefore()
    {
        var start = new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var (previousStart, previousEnd) = DateRangeRules.PreviousPeriod(start, end);

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), previousStart);
        Assert.Equal(start, previousEnd);
        Assert.Equal(end - start, previousEnd - previousStart);
    }
}
