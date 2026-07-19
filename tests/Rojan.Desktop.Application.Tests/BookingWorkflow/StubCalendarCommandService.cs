using Rojan.Desktop.Application.Calendar;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Call-recording <see cref="ICalendarCommandService"/> test double, with a switch to make <see cref="ReserveSlotAsync"/> throw - needed to exercise <c>BookingWorkflowService.CreateBookingAsync</c>'s reservation-succeeds-then-booking-fails rollback path from the booking-command side, and the reservation-itself-fails path from this side.</summary>
internal sealed class StubCalendarCommandService : ICalendarCommandService
{
    public List<(string SpecialistId, DateTimeOffset Start, DateTimeOffset End)> ReserveCalls { get; } = [];

    public List<(string SpecialistId, DateTimeOffset Start, DateTimeOffset End)> ReleaseCalls { get; } = [];

    public bool ThrowOnReserve { get; set; }

    public Task ReserveSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        if (ThrowOnReserve)
        {
            throw new InvalidOperationException("Slot is already booked.");
        }

        ReserveCalls.Add((specialistId, slotStart, slotEnd));
        return Task.CompletedTask;
    }

    public Task ReleaseSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        ReleaseCalls.Add((specialistId, slotStart, slotEnd));
        return Task.CompletedTask;
    }
}
