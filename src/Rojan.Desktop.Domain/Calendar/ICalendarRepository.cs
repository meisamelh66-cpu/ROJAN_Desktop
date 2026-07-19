namespace Rojan.Desktop.Domain.Calendar;

/// <summary>
/// Repository abstraction for calendar/availability data. Domain defines
/// the contract; Infrastructure provides the concrete implementation (a
/// fake/in-memory one for now - Phase 14 explicitly has no backend
/// integration yet, same as every other vertical slice in this app).
/// Deliberately returns raw schedule/booked-slot data only - slot
/// generation and conflict detection are Application's job (see
/// <c>Application.Calendar.CalendarQueryService</c>), not Domain's or
/// Infrastructure's, the same "return the read-set, compose in
/// Application" convention search already established across this app.
/// </summary>
public interface ICalendarRepository
{
    public Task<IReadOnlyList<WorkingSchedule>> GetWorkingSchedulesAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<TimeSlot>> GetBookedSlotsAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default);

    public Task<TimeSlot> ReserveSlotAsync(string specialistId, TimeSlot slot, CancellationToken cancellationToken = default);

    public Task ReleaseSlotAsync(string specialistId, TimeSlot slot, CancellationToken cancellationToken = default);
}
