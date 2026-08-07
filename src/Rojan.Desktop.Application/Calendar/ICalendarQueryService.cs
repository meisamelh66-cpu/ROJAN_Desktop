namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Read-only use case Presentation depends on to load calendar/availability
/// data - the only way Presentation ever reaches it, never through Domain/
/// Infrastructure directly.
///
/// Calendar/Availability Integration Phase 3: <see cref="GetDailyAvailabilityAsync"/>/
/// <see cref="GetWeeklyAvailabilityAsync"/> gained a required <c>serviceId</c>
/// parameter - ROJAN_Backend's <c>available-slots</c> engine derives slot
/// length from the selected service and has no notion of a
/// service-independent availability grid (see
/// <c>Infrastructure.Calendar.BackendCalendarAvailabilityRepository</c>).
/// <see cref="Application.Calendar.CalendarQueryService"/>, the local/EF
/// implementation, accepts and ignores it (its fixed-30-minute generation
/// never depended on a service) purely to keep compiling - it is no longer
/// the registered implementation of this interface (see
/// <c>Infrastructure.DependencyInjection.ServiceCollectionExtensions</c>'s
/// own comment for why that registration moved to Infrastructure).
/// </summary>
public interface ICalendarQueryService
{
    public Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Every bookable slot for <paramref name="specialistId"/>/<paramref name="serviceId"/> on <paramref name="scheduleDate"/>, each already excluding non-working time, schedule overrides/leaves/blocks, and existing bookings - see the registered implementation for exactly what "Available" means for the data source it's backed by.</summary>
    public Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default);

    /// <summary>Runs <see cref="GetDailyAvailabilityAsync"/>'s same per-day logic for the 7 consecutive days starting at <paramref name="weekStart"/> - the week-view foundation query (Sprint 2 Commit 3).</summary>
    public Task<WeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(string specialistId, string serviceId, DateOnly weekStart, CancellationToken cancellationToken = default);
}
