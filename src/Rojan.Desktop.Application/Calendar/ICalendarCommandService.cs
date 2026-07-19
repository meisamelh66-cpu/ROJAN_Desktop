namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Write use cases for the calendar - deliberately scoped to reserving/
/// releasing a single slot, not a full booking wizard (no customer,
/// service, or notes capture here - that remains the Bookings module's
/// job, this is the availability engine only).
/// </summary>
public interface ICalendarCommandService
{
    public Task ReserveSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default);

    public Task ReleaseSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default);
}
