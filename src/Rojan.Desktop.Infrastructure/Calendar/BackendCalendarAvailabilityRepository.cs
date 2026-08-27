using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Infrastructure.Calendar;

/// <summary>
/// Calendar/Availability Integration Phase 3: the real, backend-connected
/// <see cref="ICalendarQueryService"/> - replaces
/// <see cref="Application.Calendar.CalendarQueryService"/> (which stays in
/// the codebase, unreferenced, same convention as every earlier Fake/Ef-&gt;
/// Backend swap - see <c>BackendDashboardRepository</c>'s own doc comment).
///
/// Deliberately targets <see cref="ICalendarQueryService"/> (Application)
/// directly rather than <c>Domain.Calendar.ICalendarRepository</c>
/// (Domain) - a real deviation from every other vertical slice's
/// swap-the-repository-under-an-unchanged-Application-service pattern.
/// ROJAN_Backend's <c>available-slots</c> endpoint already performs the
/// entire slot-generation computation server-side (working hours, weekly
/// availability, overrides, leaves, blocks, live booking conflicts,
/// past-time exclusion) - reusing <c>ICalendarRepository</c>'s "return raw
/// schedule rows, generate slots in Application" shape would mean either
/// re-fetching five separate schedule endpoints per day and reimplementing
/// the backend's own <c>TimeSlotEngine</c> client-side (duplicated logic,
/// real risk of drift) or calling <c>available-slots</c> once and mapping
/// its answer directly. The latter is what this class does.
/// <c>ICalendarRepository</c> was untouched by this change at the time -
/// <c>ICalendarCommandService</c>'s implementation still depended on it
/// (and on the now-removed <c>EfCalendarRepository</c>) for its own,
/// unrelated reserve/release bookkeeping. Remediation Phase 3A (Calendar
/// Dead Code Cleanup) later removed that entire command-side chain -
/// confirmed to have zero production callers by then, since real booking
/// creation independently stopped orchestrating any Calendar reserve/
/// release step once Backend became the sole booking-conflict authority.
/// See ROJAN_DESKTOP_CALENDAR_CLEANUP_PHASE3A_REPORT_v1.md.
///
/// Honesty notes on the mapping, all deliberate:
/// <list type="bullet">
/// <item><see cref="AvailabilityStatus.Booked"/>/<see cref="AvailabilityStatus.Unavailable"/>
/// are never produced - <c>available-slots</c> returns only free windows,
/// with no signal for *why* a given moment is absent (booked, blocked, a
/// leave, outside working hours - all indistinguishable from here). Every
/// slot this class returns is <see cref="AvailabilityStatus.Available"/>.
/// This is the direct consequence of the Phase 3 product decision - a
/// slot's Booked state now comes from real <c>Booking</c> data, not a
/// value this read path fabricates.</item>
/// <item><see cref="DailyAvailabilityDto.WorkingStart"/>/<see cref="DailyAvailabilityDto.WorkingEnd"/>
/// are derived from the first/last returned slot's time-of-day, not a real
/// "does this specialist work today" signal - a fully-booked working day
/// and a genuine non-working/leave day both produce an empty slot list and
/// therefore both show as <see langword="null"/>/"not scheduled" in
/// Presentation. Backend-accurate discrimination between the two would
/// require querying the schedule-authoring endpoints separately, which
/// this class deliberately does not do (see this class's own opening
/// paragraph on why a single <c>available-slots</c> call is the correct
/// integration point).</item>
/// <item><see cref="GetScheduledSpecialistsAsync"/> now means "every active
/// specialist in this salon", not "has at least one working-hours entry" -
/// ROJAN_Backend has no cheap way to answer the narrower question without
/// querying every specialist's schedule individually, and an active
/// specialist with no schedule configured yet correctly surfaces as an
/// empty-slots day rather than being hidden from the picker.</item>
/// </list>
/// </summary>
public sealed class BackendCalendarAvailabilityRepository(
    IApiClient apiClient,
    ISalonContextService salonContextService) : ICalendarQueryService
{
    public async Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<List<SpecialistResponse>>($"/api/v1/salons/{salonId}/specialists", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load specialists (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data
            .Where(specialist => specialist.Active)
            .Select(specialist => new ScheduledSpecialistDto(specialist.Id, specialist.DisplayName))
            .OrderBy(specialist => specialist.Name, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var specialistName = await ResolveSpecialistNameAsync(salonId, specialistId, cancellationToken).ConfigureAwait(false);
        var slots = await FetchSlotsAsync(salonId, specialistId, serviceId, scheduleDate, cancellationToken).ConfigureAwait(false);

        var dtoSlots = slots
            .Select(slot => new AvailabilitySlotDto(specialistId, specialistName, ToOffset(slot.Start), ToOffset(slot.End), AvailabilityStatus.Available))
            .ToList();

        TimeSpan? workingStart = dtoSlots.Count > 0 ? dtoSlots[0].Start.TimeOfDay : null;
        TimeSpan? workingEnd = dtoSlots.Count > 0 ? dtoSlots[^1].End.TimeOfDay : null;

        return new DailyAvailabilityDto(specialistId, specialistName, scheduleDate, workingStart, workingEnd, dtoSlots);
    }

    public async Task<WeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(string specialistId, string serviceId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var days = new List<DailyAvailabilityDto>(7);
        var specialistName = string.Empty;

        for (var offset = 0; offset < 7; offset++)
        {
            var day = await GetDailyAvailabilityAsync(specialistId, serviceId, weekStart.AddDays(offset), cancellationToken).ConfigureAwait(false);
            days.Add(day);
            specialistName = day.SpecialistName;
        }

        return new WeeklyAvailabilityDto(specialistId, specialistName, weekStart, days);
    }

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to load availability for.");
    }

    /// <summary>Falls back to the raw id on a failed lookup rather than failing the whole availability read - same best-effort reasoning as <c>BackendBookingRepository.ResolveSpecialistNameAsync</c>.</summary>
    private async Task<string> ResolveSpecialistNameAsync(string salonId, string specialistId, CancellationToken cancellationToken)
    {
        var response = await apiClient
            .GetAsync<SpecialistResponse>($"/api/v1/salons/{salonId}/specialists/{specialistId}", cancellationToken)
            .ConfigureAwait(false);

        return response.IsSuccess && response.Data is not null ? response.Data.DisplayName : specialistId;
    }

    private async Task<List<TimeSlotResponse>> FetchSlotsAsync(string salonId, string specialistId, string serviceId, DateOnly date, CancellationToken cancellationToken)
    {
        var path = $"/api/v1/salons/{salonId}/specialists/{specialistId}/available-slots?serviceId={serviceId}&date={date:yyyy-MM-dd}";
        var response = await apiClient.GetAsync<List<TimeSlotResponse>>(path, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load availability for specialist '{specialistId}' on {date:yyyy-MM-dd} (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data;
    }

    /// <summary>The backend sends a timezone-less LocalDateTime - treated as the app's own local wall-clock time, matching <c>BackendBookingRepository</c>'s own established <c>DateTimeOffset.Now.Offset</c> convention.</summary>
    private static DateTimeOffset ToOffset(DateTime time) => new(time, DateTimeOffset.Now.Offset);
}
