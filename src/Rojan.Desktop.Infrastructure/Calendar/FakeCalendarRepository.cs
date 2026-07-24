using Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Infrastructure.Calendar;

/// <summary>
/// In-memory <see cref="ICalendarRepository"/> providing static sample
/// data - Phase 14 explicitly has no backend integration yet, same as
/// every other vertical slice in this app. Instance (not static) mutable
/// state for booked slots, same reasoning as <c>Customers.FakeCustomerRepository</c>:
/// this fake has real reserve/release commands, so it needs to remember
/// writes for the app's lifetime - registered as a DI singleton (see
/// Infrastructure's ServiceCollectionExtensions). Working schedules and
/// specialist names match the specialists already seeded in
/// <c>Specialists.FakeSpecialistRepository</c> for a cohesive demo - not a
/// real cross-slice link, just consistent naming. Seed booked slots are
/// computed relative to "today" (next occurrence of a given weekday) so
/// the demo always shows a real conflict on an upcoming working day, no
/// matter what date the app happens to be run on. Each specialist's
/// <see cref="WorkingSchedule.Breaks"/> (Sprint 2 Commit 1) gets one seeded
/// lunch window per working day, computed the same "next occurrence"
/// way and chosen not to overlap the seed booked conflicts above, so the
/// demo shows a real Unavailable break alongside the real Booked conflict
/// without one masking the other. The small artificial delays are
/// deliberate, same reasoning as every other fake repository: without
/// them, Loading states would never actually be observable when running
/// the app.
/// </summary>
public sealed class FakeCalendarRepository : ICalendarRepository
{
    private readonly List<WorkingSchedule> _schedules;
    private readonly Dictionary<string, List<TimeSlot>> _bookedSlotsBySpecialist;

    public FakeCalendarRepository()
    {
        _schedules =
        [
            .. BuildWeekSchedule("specialist-1", "کیانا رادمنش",
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), "schedule-1",
                new TimeSpan(13, 0, 0), new TimeSpan(13, 30, 0)),
            .. BuildWeekSchedule("specialist-2", "سارا امینی",
                [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
                new TimeSpan(10, 0, 0), new TimeSpan(18, 0, 0), "schedule-6",
                new TimeSpan(13, 30, 0), new TimeSpan(14, 0, 0)),
            .. BuildWeekSchedule("specialist-3", "مهسا کریمی",
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                new TimeSpan(9, 30, 0), new TimeSpan(17, 30, 0), "schedule-11",
                new TimeSpan(12, 0, 0), new TimeSpan(12, 30, 0)),
        ];

        _bookedSlotsBySpecialist = new Dictionary<string, List<TimeSlot>>
        {
            ["specialist-1"] =
            [
                MakeSlot(NextOccurrence(DayOfWeek.Monday), new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0)),
                MakeSlot(NextOccurrence(DayOfWeek.Wednesday), new TimeSpan(15, 0, 0), new TimeSpan(15, 30, 0)),
            ],
            ["specialist-2"] =
            [
                MakeSlot(NextOccurrence(DayOfWeek.Tuesday), new TimeSpan(11, 0, 0), new TimeSpan(11, 30, 0)),
            ],
            ["specialist-3"] =
            [
                MakeSlot(NextOccurrence(DayOfWeek.Friday), new TimeSpan(13, 0, 0), new TimeSpan(14, 0, 0)),
            ],
        };
    }

    public async Task<IReadOnlyList<WorkingSchedule>> GetWorkingSchedulesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _schedules.ToList();
    }

    public async Task<IReadOnlyList<TimeSlot>> GetBookedSlotsAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);

        if (!_bookedSlotsBySpecialist.TryGetValue(specialistId, out var slots))
        {
            return [];
        }

        return slots.Where(slot => DateOnly.FromDateTime(slot.Start.DateTime) == scheduleDate).ToList();
    }

    public async Task<TimeSlot> ReserveSlotAsync(string specialistId, TimeSlot slot, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);

        if (!_bookedSlotsBySpecialist.TryGetValue(specialistId, out var slots))
        {
            slots = [];
            _bookedSlotsBySpecialist[specialistId] = slots;
        }

        slots.Add(slot);
        return slot;
    }

    public async Task ReleaseSlotAsync(string specialistId, TimeSlot slot, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);

        if (_bookedSlotsBySpecialist.TryGetValue(specialistId, out var slots))
        {
            slots.RemoveAll(existing => existing.Start == slot.Start && existing.End == slot.End);
        }
    }

    private static IEnumerable<WorkingSchedule> BuildWeekSchedule(
        string specialistId, string specialistName, IReadOnlyList<DayOfWeek> days, TimeSpan startTime, TimeSpan endTime, string idPrefix,
        TimeSpan? breakStartTime = null, TimeSpan? breakEndTime = null)
    {
        for (var i = 0; i < days.Count; i++)
        {
            IReadOnlyList<TimeSlot> breaks = breakStartTime.HasValue && breakEndTime.HasValue
                ? [MakeSlot(NextOccurrence(days[i]), breakStartTime.Value, breakEndTime.Value)]
                : [];

            yield return new WorkingSchedule($"{idPrefix}-{i}", specialistId, specialistName, days[i], startTime, endTime, breaks);
        }
    }

    private static TimeSlot MakeSlot(DateOnly date, TimeSpan startTime, TimeSpan endTime)
    {
        var offset = DateTimeOffset.Now.Offset;
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, startTime.Hours, startTime.Minutes, 0, offset);
        var end = new DateTimeOffset(date.Year, date.Month, date.Day, endTime.Hours, endTime.Minutes, 0, offset);
        return new TimeSlot(start, end);
    }

    /// <summary>The next date (strictly after today) that falls on <paramref name="dayOfWeek"/> - always looks forward, never returns today even if today already matches.</summary>
    private static DateOnly NextOccurrence(DayOfWeek dayOfWeek)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysUntil = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        daysUntil = daysUntil == 0 ? 7 : daysUntil;
        return today.AddDays(daysUntil);
    }
}
