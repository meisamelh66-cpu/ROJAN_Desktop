using System.Globalization;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Notifications;

/// <summary>Phase 27: Enterprise Notification Center. "5 minutes ago"-style formatting for a notification's timestamp - computed once at list-refresh time (not a live-ticking clock), the same acceptable-staleness tradeoff every other non-critical relative timestamp in this app already makes.</summary>
public static class RelativeTimeFormatter
{
    public static string Format(DateTimeOffset createdAt, DateTimeOffset now)
    {
        var elapsed = now - createdAt;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return Strings.Common_JustNow;
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return FormatValue(Strings.Common_MinutesAgoFormat, (int)elapsed.TotalMinutes);
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return FormatValue(Strings.Common_HoursAgoFormat, (int)elapsed.TotalHours);
        }

        return FormatValue(Strings.Common_DaysAgoFormat, (int)elapsed.TotalDays);
    }

    private static string FormatValue(string template, int value) =>
        template.Replace("{0}", value.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal);
}
