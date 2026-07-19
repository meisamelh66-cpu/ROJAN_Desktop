namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>Input to <see cref="IBookingWorkflowService.CreateBookingAsync"/> - unlike <c>Bookings.CreateBookingRequest</c>, every id is required: this is the fully-resolved, wizard-driven creation path, not the Bookings page's free-text quick-add form.</summary>
public sealed record CreateBookingWorkflowRequest(
    string CustomerId,
    string CustomerName,
    string ServiceId,
    string ServiceName,
    int DurationMinutes,
    string Price,
    string SpecialistId,
    string SpecialistName,
    DateTimeOffset SlotStart,
    string Notes);
