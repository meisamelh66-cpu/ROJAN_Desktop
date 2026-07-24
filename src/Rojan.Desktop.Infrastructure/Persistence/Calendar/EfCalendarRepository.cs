using Microsoft.EntityFrameworkCore;
using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>
/// Sprint 6 Commit 6: real EF Core-backed <see cref="DomainCalendar.ICalendarRepository"/> -
/// the fifth (and, for Sprint 6, last) Domain module moved off its
/// <c>Fake*Repository</c> onto <see cref="RojanDbContext"/>, same shape
/// <see cref="Customers.EfCustomerRepository"/>/<see cref="Specialists.EfSpecialistRepository"/>/
/// <see cref="Services.EfServiceRepository"/>/<see cref="Bookings.EfBookingRepository"/>
/// already establish (short-lived <see cref="RojanDbContext"/> per call
/// via <see cref="IDbContextFactory{TContext}"/>, registered as a DI
/// singleton).
///
/// Unlike Customers/Specialists/Bookings, there is no
/// create/update-schedule method at all - see
/// <see cref="DomainCalendar.ICalendarRepository"/>'s own doc comment
/// (Phase 14 scoped this module to reading schedules plus
/// reserving/releasing slots, not schedule authoring). A real, known
/// consequence, more severe than the Services gap (Commit 4): a fresh
/// SQLite database has zero <see cref="DomainCalendar.WorkingSchedule"/>
/// rows, and <c>Application.Calendar.CalendarQueryService.GetDailyAvailabilityAsync</c>/
/// <c>GetWeeklyAvailabilityAsync</c> both throw
/// <see cref="InvalidOperationException"/> ("has no working schedule")
/// the moment any specialist is looked up with none - not merely an empty
/// result. That gap is pre-existing (schedule authoring was never in
/// scope for this vertical slice; <c>FakeCalendarRepository</c>'s
/// schedules were always just hardcoded seed data too) and is not
/// something this commit's scope - matching the existing repository
/// contract exactly - can or should invent a fix for.
///
/// <see cref="GetWorkingSchedulesAsync"/> assembles each
/// <see cref="DomainCalendar.WorkingSchedule.Breaks"/> list in-memory
/// (two separate queries, joined via <see cref="ILookup{TKey,TElement}"/>)
/// since neither entity has a navigation property (same "no navigation
/// property, only an FK" convention every other Ef*Repository in this app
/// follows) - <see cref="DomainCalendar.WorkingSchedule"/> itself embeds
/// Breaks directly, unlike Customer/Specialist's child collections, which
/// are each their own separate repository method.
/// <see cref="GetBookedSlotsAsync"/> filters by date client-side after
/// fetching by specialist - the same DateTimeOffset-ordering-adjacent
/// SQLite EF Core translation limit already worked around in
/// <see cref="Customers.EfCustomerRepository"/> (there it was ORDER BY;
/// <c>DateOnly.FromDateTime(...)</c> here is a client-only conversion
/// EF Core cannot translate to SQL at all), and exactly matches
/// <c>FakeCalendarRepository.GetBookedSlotsAsync</c>'s own
/// materialize-then-filter shape.
/// </summary>
public sealed class EfCalendarRepository : DomainCalendar.ICalendarRepository
{
    private readonly IDbContextFactory<RojanDbContext> _contextFactory;

    public EfCalendarRepository(IDbContextFactory<RojanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<DomainCalendar.WorkingSchedule>> GetWorkingSchedulesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var scheduleEntities = await context.WorkingSchedules.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var breakEntities = await context.WorkingScheduleBreaks.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var breaksBySchedule = breakEntities.ToLookup(entity => entity.WorkingScheduleId);

        return scheduleEntities
            .Select(schedule => CalendarEntityMapper.MapToDomain(
                schedule,
                breaksBySchedule[schedule.Id].Select(CalendarEntityMapper.MapToDomain).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<DomainCalendar.TimeSlot>> GetBookedSlotsAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.ReservedSlots
            .AsNoTracking()
            .Where(slot => slot.SpecialistId == specialistId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities
            .Where(slot => DateOnly.FromDateTime(slot.Start.DateTime) == scheduleDate)
            .Select(CalendarEntityMapper.MapToDomain)
            .ToList();
    }

    public async Task<DomainCalendar.TimeSlot> ReserveSlotAsync(string specialistId, DomainCalendar.TimeSlot slot, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.ReservedSlots.Add(CalendarEntityMapper.MapToEntity(specialistId, slot));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return slot;
    }

    public async Task ReleaseSlotAsync(string specialistId, DomainCalendar.TimeSlot slot, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.ReservedSlots
            .FirstOrDefaultAsync(
                existing => existing.SpecialistId == specialistId && existing.Start == slot.Start && existing.End == slot.End,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        context.ReservedSlots.Remove(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
