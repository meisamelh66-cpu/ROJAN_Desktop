namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>
/// EF Core persistence model for one recurring blocked window (e.g.
/// lunch) within a <see cref="WorkingScheduleEntity"/> - persists
/// <see cref="Domain.Calendar.WorkingSchedule.Breaks"/>, each a
/// <see cref="Domain.Calendar.TimeSlot"/> value with no identity of its
/// own. No synthetic id: <see cref="WorkingScheduleId"/>/<see cref="Start"/>/
/// <see cref="End"/> together form this entity's key (see
/// <see cref="WorkingScheduleBreakEntityConfiguration"/>) - the same
/// non-invented identity a <see cref="Domain.Calendar.TimeSlot"/> already
/// has. Unlike Bookings' cross-slice references, this genuinely is a
/// within-module parent-child relationship (a break cannot exist without
/// the working schedule it belongs to), so - unlike
/// <c>Bookings.BookingEntity</c> - a real foreign key with cascade delete
/// is appropriate here.
/// </summary>
public sealed class WorkingScheduleBreakEntity
{
    public string WorkingScheduleId { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }
}
