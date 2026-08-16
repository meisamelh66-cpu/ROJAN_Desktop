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

    /// <summary>
    /// Reception Stabilization Sprint: the Wizard's "Walk-in" entry point - creates a new CRM
    /// customer (via the same <c>Customers.ICustomerCommandService.CreateCustomerAsync</c> every
    /// other customer-creation path already uses, so it is subject to the same
    /// <c>Permission.CustomerEdit</c> gate Reception already has) and returns it as a
    /// <see cref="WorkflowCustomerOptionDto"/> with <see cref="WorkflowCustomerOptionDto.IsLinkedToAccount"/>
    /// always <see langword="false"/> - a customer created this way has no backend user account by
    /// definition, the same "walk-in" distinction the rest of this interface's docs describe.
    /// </summary>
    public Task<WorkflowCustomerOptionDto> CreateGuestCustomerAsync(string fullName, string phone, CancellationToken cancellationToken = default);

    /// <summary>Available slots for the given specialist/service/date, delegating slot generation to <c>Calendar.ICalendarQueryService</c> - <paramref name="serviceId"/> is required because slot length is derived from the selected service (Calendar/Availability Integration Phase 3).</summary>
    public Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default);

    /// <summary>Reserves the chosen calendar slot, then creates the booking; if booking creation fails, the calendar reservation is rolled back (released) since there is no database transaction spanning both writes.</summary>
    public Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancels a booking and releases its calendar slot, if it had a real specialist id (bookings created via the Bookings page's free-text quick-add form never reserved a calendar slot, so there is nothing to release).</summary>
    public Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves an existing active booking to <paramref name="newSlotStart"/> for the same
    /// specialist/duration. If the booking has a real Calendar reservation, the new slot is
    /// reserved <em>before</em> the old one is released and before the booking record itself is
    /// updated - if the new slot is unavailable, the original booking and its original
    /// reservation are left completely untouched; if the booking update fails after the new slot
    /// was reserved, that new reservation is released so it doesn't stay stuck as Booked (Sprint 3
    /// Commit 6).
    /// </summary>
    public Task<BookingConfirmationDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newSlotStart, CancellationToken cancellationToken = default);
}
