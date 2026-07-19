using System.Globalization;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>Resolves an <see cref="AnalyticsPeriod"/> into a concrete date range (anchored to <see cref="DateTimeOffset.Now"/>) plus a display label - the one place "what does Daily/Weekly/Monthly mean" is defined, shared by the KPI Engine, Analytics Dashboard, and the three dashboard reports.</summary>
internal static class AnalyticsPeriods
{
    public static (DateTimeOffset Start, DateTimeOffset End, string Label) Resolve(AnalyticsPeriod period)
    {
        var now = DateTimeOffset.Now;
        return period switch
        {
            AnalyticsPeriod.Daily => (now.Date, now.Date.AddDays(1).AddTicks(-1), "Today"),
            AnalyticsPeriod.Weekly => (StartOfWeek(now), StartOfWeek(now).AddDays(7).AddTicks(-1), "This Week"),
            AnalyticsPeriod.Monthly => (StartOfMonth(now), StartOfMonth(now).AddMonths(1).AddTicks(-1), "This Month"),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown analytics period."),
        };
    }

    public static (DateTimeOffset Start, DateTimeOffset End) PreviousPeriod(AnalyticsPeriod period)
    {
        var (start, end, _) = Resolve(period);
        var length = end - start;
        return (start - length, start.AddTicks(-1));
    }

    private static DateTimeOffset StartOfWeek(DateTimeOffset value)
    {
        var diff = ((int)value.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return value.Date.AddDays(-diff);
    }

    private static DateTimeOffset StartOfMonth(DateTimeOffset value) =>
        new(value.Year, value.Month, 1, 0, 0, 0, value.Offset);

    public static string FormatMonthLabel(int month, int year) =>
        new DateOnly(year, month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
}
