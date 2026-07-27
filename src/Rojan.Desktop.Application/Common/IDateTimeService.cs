namespace Rojan.Desktop.Application.Common;

/// <summary>
/// Product requirement: a single centralized source of system date/time
/// and time zone conversion - Domain stays untouched (every Domain entity
/// already takes its timestamp as a caller-supplied constructor/method
/// parameter rather than reading the clock itself, so it never needed
/// this), but Application/Infrastructure/Presentation all currently read
/// <see cref="DateTime.Now"/>/<see cref="DateTimeOffset.UtcNow"/> directly
/// in dozens of places. This interface lives in Application (not
/// Presentation) specifically so Application and Infrastructure - which
/// cannot depend on Presentation under this solution's Clean Architecture
/// dependency direction - can consume it too; the culture/calendar-display
/// half of the same product requirement is a screens-only concern and
/// lives separately as <c>Presentation.Localization.ICalendarService</c>.
///
/// <see cref="ConvertToTimeZone"/> takes the target time zone as a plain
/// string id rather than a stronger type deliberately: no call site
/// exists yet, but <c>Domain.Organizations.Organization.TimeZone</c> is
/// already a persisted string field (currently unused for conversion) -
/// this interface shape lets a future commit thread that field straight
/// into this method with zero interface change.
/// </summary>
public interface IDateTimeService
{
    /// <summary>The current moment in the local system time zone.</summary>
    public DateTimeOffset Now { get; }

    /// <summary>The current moment in UTC.</summary>
    public DateTimeOffset UtcNow { get; }

    /// <summary>The local system's time zone.</summary>
    public TimeZoneInfo LocalTimeZone { get; }

    /// <summary>Converts <paramref name="value"/> into <paramref name="timeZoneId"/> (a Windows or IANA time zone id, per <see cref="TimeZoneInfo.FindSystemTimeZoneById"/>). Throws <see cref="TimeZoneNotFoundException"/> for an unrecognized id - the same fail-fast posture the rest of this codebase already takes for invalid configuration, not a workaround-friendly silent fallback.</summary>
    public DateTimeOffset ConvertToTimeZone(DateTimeOffset value, string timeZoneId);
}
