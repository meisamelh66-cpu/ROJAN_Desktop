using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>
/// Domain&lt;-&gt;persistence-entity mapping for the Calendar vertical slice -
/// internal, only <see cref="EfCalendarRepository"/> (and its own tests,
/// for arrange-time seeding - <see cref="DomainCalendar.ICalendarRepository"/> has no
/// create/update method for <see cref="DomainCalendar.WorkingSchedule"/>
/// at all) call it, same convention as every other Domain&lt;-&gt;entity
/// mapper in this codebase (<see cref="Customers.CustomerEntityMapper"/>).
/// </summary>
internal static class CalendarEntityMapper
{
    public static DomainCalendar.WorkingSchedule MapToDomain(WorkingScheduleEntity entity, IReadOnlyList<DomainCalendar.TimeSlot> breaks) => new(
        entity.Id,
        entity.SpecialistId,
        entity.SpecialistName,
        entity.DayOfWeek,
        entity.StartTime,
        entity.EndTime,
        breaks);

    public static WorkingScheduleEntity MapToEntity(DomainCalendar.WorkingSchedule schedule) => new()
    {
        Id = schedule.Id,
        SpecialistId = schedule.SpecialistId,
        SpecialistName = schedule.SpecialistName,
        DayOfWeek = schedule.DayOfWeek,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
    };

    public static DomainCalendar.TimeSlot MapToDomain(WorkingScheduleBreakEntity entity) =>
        new(entity.Start, entity.End);

    public static WorkingScheduleBreakEntity MapBreakToEntity(string workingScheduleId, DomainCalendar.TimeSlot timeSlot) => new()
    {
        WorkingScheduleId = workingScheduleId,
        Start = timeSlot.Start,
        End = timeSlot.End,
    };

    public static DomainCalendar.TimeSlot MapToDomain(ReservedSlotEntity entity) =>
        new(entity.Start, entity.End);

    public static ReservedSlotEntity MapToEntity(string specialistId, DomainCalendar.TimeSlot slot) => new()
    {
        SpecialistId = specialistId,
        Start = slot.Start,
        End = slot.End,
    };
}
