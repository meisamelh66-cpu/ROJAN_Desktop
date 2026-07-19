using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Default <see cref="ICalendarCommandService"/> implementation.
/// <see cref="ReserveSlotAsync"/> re-checks for a conflict immediately
/// before writing - Presentation only ever offers Reserve on a slot
/// already shown as Available, but that snapshot can go stale (another
/// reservation landing first), so this is a real write-time guarantee,
/// not just a display-time convenience.
/// </summary>
public sealed class CalendarCommandService : ICalendarCommandService
{
    private readonly DomainCalendar.ICalendarRepository _repository;

    public CalendarCommandService(DomainCalendar.ICalendarRepository repository)
    {
        _repository = repository;
    }

    public async Task ReserveSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        var scheduleDate = DateOnly.FromDateTime(slotStart.DateTime);
        var bookedSlots = await _repository.GetBookedSlotsAsync(specialistId, scheduleDate, cancellationToken).ConfigureAwait(true);

        if (bookedSlots.Any(booked => slotStart < booked.End && booked.Start < slotEnd))
        {
            throw new InvalidOperationException("This time slot is already booked.");
        }

        await _repository.ReserveSlotAsync(specialistId, new DomainCalendar.TimeSlot(slotStart, slotEnd), cancellationToken).ConfigureAwait(true);
    }

    public Task ReleaseSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default) =>
        _repository.ReleaseSlotAsync(specialistId, new DomainCalendar.TimeSlot(slotStart, slotEnd), cancellationToken);
}
