namespace Rojan.Desktop.Domain.Reporting;

/// <summary>Genuine Domain validation for the Filter Engine's <see cref="FilterType.DateRange"/> filter.</summary>
public static class DateRangeRules
{
    public static bool IsValidRange(DateTimeOffset start, DateTimeOffset end) => start <= end;

    /// <summary>The prior period of equal length immediately before <paramref name="start"/> - what every trend-comparison KPI/report compares its current period against.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) PreviousPeriod(DateTimeOffset start, DateTimeOffset end)
    {
        var length = end - start;
        return (start - length, start);
    }
}
