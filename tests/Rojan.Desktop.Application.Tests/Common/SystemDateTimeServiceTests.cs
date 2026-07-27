using Rojan.Desktop.Application.Common;

namespace Rojan.Desktop.Application.Tests.Common;

public sealed class SystemDateTimeServiceTests
{
    private readonly SystemDateTimeService _sut = new();

    [Fact]
    public void Now_ReturnsCurrentLocalMoment()
    {
        var before = DateTimeOffset.Now;

        var result = _sut.Now;

        var after = DateTimeOffset.Now;
        Assert.InRange(result, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void UtcNow_ReturnsCurrentUtcMoment()
    {
        var before = DateTimeOffset.UtcNow;

        var result = _sut.UtcNow;

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(result, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void LocalTimeZone_ReturnsSystemLocalTimeZone()
    {
        Assert.Equal(TimeZoneInfo.Local, _sut.LocalTimeZone);
    }

    [Fact]
    public void ConvertToTimeZone_ToUtc_ProducesEquivalentInstantWithZeroOffset()
    {
        var value = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.FromHours(3));

        var result = _sut.ConvertToTimeZone(value, "UTC");

        Assert.Equal(value.ToUniversalTime(), result);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void ConvertToTimeZone_UnrecognizedTimeZoneId_ThrowsTimeZoneNotFoundException()
    {
        var value = DateTimeOffset.UtcNow;

        Assert.Throws<TimeZoneNotFoundException>(() => _sut.ConvertToTimeZone(value, "Not/A_Real_Zone"));
    }
}
