namespace Rojan.Desktop.Application.Calendar;

/// <summary>Read-only use case Presentation depends on to load calendar/availability data - the only way Presentation ever reaches it, never through Domain/Infrastructure directly.</summary>
public interface ICalendarQueryService
{
    public Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Generates every 30-minute slot within the specialist's working hours for <paramref name="scheduleDate"/> and marks each Available, Booked (against existing booked ranges), or Unavailable (against the specialist's recurring breaks) - the "generate available time slots" plus "detect booking conflicts/blocked time" behavior, exposed as one query.</summary>
    public Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default);

    /// <summary>Runs <see cref="GetDailyAvailabilityAsync"/>'s same generation/conflict/break logic once per day for the 7 consecutive days starting at <paramref name="weekStart"/> - the week-view foundation query (Sprint 2 Commit 3).</summary>
    public Task<WeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(string specialistId, DateOnly weekStart, CancellationToken cancellationToken = default);
}
