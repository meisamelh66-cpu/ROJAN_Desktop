namespace Rojan.Desktop.Application.Calendar;

/// <summary>Read-only use case Presentation depends on to load calendar/availability data - the only way Presentation ever reaches it, never through Domain/Infrastructure directly.</summary>
public interface ICalendarQueryService
{
    public Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Generates every 30-minute slot within the specialist's working hours for <paramref name="scheduleDate"/> and marks each Available or Booked against existing booked ranges - the "generate available time slots" plus "detect booking conflicts" behavior, exposed as one query.</summary>
    public Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default);
}
