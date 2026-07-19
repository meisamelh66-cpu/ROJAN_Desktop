namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>
/// The Booking Wizard's use cases - the only Application service in this
/// codebase that coordinates across multiple other vertical slices
/// (Customers, Services, Specialists, Calendar, Bookings). This is normal
/// Clean Architecture use-case orchestration: it composes other
/// Application service interfaces, so it never needs Domain from a slice
/// other than its own (<c>Domain.Bookings</c>) and does not weaken the
/// Domain-layer vertical-slice independence every other phase has
/// maintained.
/// </summary>
public interface IBookingWorkflowService
{
    /// <summary>The customers/services/specialists the wizard's picker steps offer - services and specialists are filtered to Active only.</summary>
    public Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Available (unbooked) slots for the given specialist and date, delegating slot generation and conflict detection to <c>Calendar.ICalendarQueryService</c>.</summary>
    public Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default);

    /// <summary>Reserves the chosen calendar slot, then creates the booking; if booking creation fails, the calendar reservation is rolled back (released) since there is no database transaction spanning both writes.</summary>
    public Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancels a booking and releases its calendar slot, if it had a real specialist id (bookings created via the Bookings page's free-text quick-add form never reserved a calendar slot, so there is nothing to release).</summary>
    public Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default);
}
