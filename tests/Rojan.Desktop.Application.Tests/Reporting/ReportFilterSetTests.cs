using Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class ReportFilterSetTests
{
    [Fact]
    public void DateRange_WithNoDateRangeFilter_ReturnsNull()
    {
        var filterSet = new ReportFilterSet([]);

        Assert.Null(filterSet.DateRange);
    }

    [Fact]
    public void DateRange_WithValidFilter_ParsesStartAndEnd()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var filterSet = new ReportFilterSet([new ReportFilterDto(FilterType.DateRange, $"{start:O}|{end:O}", "range")]);

        Assert.Equal(start, filterSet.DateRange!.Value.Start);
        Assert.Equal(end, filterSet.DateRange!.Value.End);
    }

    [Fact]
    public void DateRange_WithMalformedValue_ReturnsNull()
    {
        var filterSet = new ReportFilterSet([new ReportFilterDto(FilterType.DateRange, "not-a-date-range", "range")]);

        Assert.Null(filterSet.DateRange);
    }

    [Fact]
    public void IsWithinDateRange_WithNoFilterApplied_AlwaysReturnsTrue()
    {
        var filterSet = new ReportFilterSet([]);

        Assert.True(filterSet.IsWithinDateRange(DateTimeOffset.Now));
    }

    [Fact]
    public void IsWithinDateRange_WithValueInsideRange_ReturnsTrue()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var filterSet = new ReportFilterSet([new ReportFilterDto(FilterType.DateRange, $"{start:O}|{end:O}", "range")]);

        Assert.True(filterSet.IsWithinDateRange(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void IsWithinDateRange_WithValueOutsideRange_ReturnsFalse()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var filterSet = new ReportFilterSet([new ReportFilterDto(FilterType.DateRange, $"{start:O}|{end:O}", "range")]);

        Assert.False(filterSet.IsWithinDateRange(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Matches_WithNoFilterForThatType_AlwaysReturnsTrue()
    {
        var filterSet = new ReportFilterSet([]);

        Assert.True(filterSet.Matches(FilterType.Specialist, "anything"));
    }

    [Fact]
    public void Matches_WithMatchingValue_ReturnsTrue()
    {
        var filterSet = new ReportFilterSet([new ReportFilterDto(FilterType.Specialist, "specialist-1", "label")]);

        Assert.True(filterSet.Matches(FilterType.Specialist, "specialist-1"));
    }

    [Fact]
    public void Matches_WithNonMatchingValue_ReturnsFalse()
    {
        var filterSet = new ReportFilterSet([new ReportFilterDto(FilterType.Specialist, "specialist-1", "label")]);

        Assert.False(filterSet.Matches(FilterType.Specialist, "specialist-2"));
    }
}
