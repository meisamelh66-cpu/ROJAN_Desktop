namespace Rojan.Desktop.Application.Common;

/// <summary>Default <see cref="IDateTimeService"/> - thin wrapper over the BCL clock/time-zone APIs, pure computation with no I/O, so (same reasoning <c>Presentation.Localization.ICultureService</c>'s own doc comment gives for staying in Presentation rather than Shell) it is registered and implemented directly in this layer rather than deferred to Infrastructure.</summary>
public sealed class SystemDateTimeService : IDateTimeService
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    public DateTimeOffset ConvertToTimeZone(DateTimeOffset value, string timeZoneId) =>
        TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
}
