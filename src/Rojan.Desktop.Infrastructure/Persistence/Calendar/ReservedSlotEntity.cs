namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>
/// EF Core persistence model for one specialist's booked/reserved time
/// range - what <see cref="Domain.Calendar.ICalendarRepository.GetBookedSlotsAsync"/>/
/// <see cref="Domain.Calendar.ICalendarRepository.ReserveSlotAsync"/>/
/// <see cref="Domain.Calendar.ICalendarRepository.ReleaseSlotAsync"/>
/// actually operate on - Domain has no named type for this concept
/// (a bare <c>(specialistId, TimeSlot)</c> pair through the repository
/// method signatures), so this is the table those three methods need to
/// exist at all, not a new Domain relationship. No synthetic id:
/// <see cref="SpecialistId"/>/<see cref="Start"/>/<see cref="End"/>
/// together form this entity's key (see
/// <see cref="ReservedSlotEntityConfiguration"/>) - the exact same triple
/// <c>ReleaseSlotAsync</c> already uses to identify which reservation to
/// remove, so no invented identity, just the one the contract already
/// implies. <see cref="SpecialistId"/> is a free-form, unvalidated
/// reference, same reasoning as <see cref="WorkingScheduleEntity.SpecialistId"/> -
/// no foreign key to the Specialists table.
/// </summary>
public sealed class ReservedSlotEntity
{
    public string SpecialistId { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }
}
