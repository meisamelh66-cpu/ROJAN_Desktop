using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Default <see cref="ICalendarQueryService"/> implementation. Owns the
/// two business rules this phase exists for: generating fixed 30-minute
/// slots across a specialist's working hours, and marking each one
/// Booked/Available by checking it against existing booked ranges
/// (conflict detection) - both deliberately live here, not in Domain or
/// Infrastructure, the same "Application owns the composition, Domain/
/// Infrastructure stay dumb data providers" rule every other vertical
/// slice's search/profile logic already follows.
///
/// Calendar/Availability Integration Phase 3: no longer the registered
/// <see cref="ICalendarQueryService"/> implementation - stays in the
/// codebase, unreferenced by DI, same convention as every earlier Fake/Ef
/// implementation this app has retired in favor of a Backend* one (see
/// <c>Infrastructure.Calendar.BackendCalendarAvailabilityRepository</c>'s
/// own doc comment). Kept alive here purely so its own tests keep exercising
/// real generation logic; the <c>serviceId</c> parameter on
/// <see cref="GetDailyAvailabilityAsync"/>/<see cref="GetWeeklyAvailabilityAsync"/>
/// is accepted and ignored, since this class's fixed-30-minute local
/// generation never depended on a service duration to begin with.
/// </summary>
public sealed class CalendarQueryService : ICalendarQueryService
{
    private static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(30);

    private readonly DomainCalendar.ICalendarRepository _repository;

    public CalendarQueryService(DomainCalendar.ICalendarRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ScheduledSpecialistDto>> GetScheduledSpecialistsAsync(CancellationToken cancellationToken = default)
    {
        var schedules = await _repository.GetWorkingSchedulesAsync(cancellationToken).ConfigureAwait(true);
        return schedules
            .Select(schedule => new ScheduledSpecialistDto(schedule.SpecialistId, schedule.SpecialistName))
            .Distinct()
            .OrderBy(specialist => specialist.Name, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<DailyAvailabilityDto> GetDailyAvailabilityAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default)
    {
        var schedules = await _repository.GetWorkingSchedulesAsync(cancellationToken).ConfigureAwait(true);
        var specialistSchedules = GetSpecialistSchedulesOrThrow(schedules, specialistId);

        return await BuildDailyAvailabilityAsync(specialistId, specialistSchedules, scheduleDate, cancellationToken).ConfigureAwait(true);
    }

    public async Task<WeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(string specialistId, string serviceId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var schedules = await _repository.GetWorkingSchedulesAsync(cancellationToken).ConfigureAwait(true);
        var specialistSchedules = GetSpecialistSchedulesOrThrow(schedules, specialistId);
        var specialistName = specialistSchedules[0].SpecialistName;

        var days = new List<DailyAvailabilityDto>(7);
        for (var offset = 0; offset < 7; offset++)
        {
            var date = weekStart.AddDays(offset);
            days.Add(await BuildDailyAvailabilityAsync(specialistId, specialistSchedules, date, cancellationToken).ConfigureAwait(true));
        }

        return new WeeklyAvailabilityDto(specialistId, specialistName, weekStart, days);
    }

    private static List<DomainCalendar.WorkingSchedule> GetSpecialistSchedulesOrThrow(IReadOnlyList<DomainCalendar.WorkingSchedule> schedules, string specialistId)
    {
        var specialistSchedules = schedules.Where(schedule => schedule.SpecialistId == specialistId).ToList();

        if (specialistSchedules.Count == 0)
        {
            throw new InvalidOperationException($"Specialist '{specialistId}' has no working schedule.");
        }

        return specialistSchedules;
    }

    private async Task<DailyAvailabilityDto> BuildDailyAvailabilityAsync(
        string specialistId, List<DomainCalendar.WorkingSchedule> specialistSchedules, DateOnly scheduleDate, CancellationToken cancellationToken)
    {
        var specialistName = specialistSchedules[0].SpecialistName;
        var todaySchedule = specialistSchedules.FirstOrDefault(schedule => schedule.DayOfWeek == scheduleDate.DayOfWeek);

        if (todaySchedule is null)
        {
            return new DailyAvailabilityDto(specialistId, specialistName, scheduleDate, null, null, []);
        }

        var bookedSlots = await _repository.GetBookedSlotsAsync(specialistId, scheduleDate, cancellationToken).ConfigureAwait(true);

        var slots = new List<AvailabilitySlotDto>();
        for (var slotStart = todaySchedule.StartTime; slotStart + SlotDuration <= todaySchedule.EndTime; slotStart += SlotDuration)
        {
            var start = ToDateTimeOffset(scheduleDate, slotStart);
            var end = start + SlotDuration;

            // A break takes precedence over an overlapping booked range - see
            // AvailabilityStatus's own doc comment for why (a break is
            // blocked-by-policy time, not a real reservation).
            var status = todaySchedule.Breaks.Any(brk => Overlaps(start, end, brk))
                ? DomainCalendar.AvailabilityStatus.Unavailable
                : bookedSlots.Any(booked => Overlaps(start, end, booked))
                    ? DomainCalendar.AvailabilityStatus.Booked
                    : DomainCalendar.AvailabilityStatus.Available;

            slots.Add(CalendarMapper.MapSlot(new DomainCalendar.AvailabilitySlot(specialistId, specialistName, start, end, status)));
        }

        return new DailyAvailabilityDto(specialistId, specialistName, scheduleDate, todaySchedule.StartTime, todaySchedule.EndTime, slots);
    }

    private static bool Overlaps(DateTimeOffset start, DateTimeOffset end, DomainCalendar.TimeSlot booked) =>
        start < booked.End && booked.Start < end;

    private static DateTimeOffset ToDateTimeOffset(DateOnly scheduleDate, TimeSpan time) =>
        new(scheduleDate.Year, scheduleDate.Month, scheduleDate.Day, time.Hours, time.Minutes, 0, DateTimeOffset.Now.Offset);
}
