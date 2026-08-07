using Rojan.Desktop.Application.BookingWorkflow;

namespace Rojan.Desktop.Presentation.Tests.BookingWorkflow;

/// <summary>Configurable, call-recording <see cref="IBookingWorkflowService"/> test double - same reasoning as Calendar.StubCalendarQueryService/StubCalendarCommandService.</summary>
internal sealed class StubBookingWorkflowService : IBookingWorkflowService
{
    private readonly Func<CancellationToken, Task<BookingOptionsDto>> _getOptions;
    private readonly Func<string, string, DateOnly, CancellationToken, Task<IReadOnlyList<WorkflowSlotDto>>> _getSlots;
    private readonly Func<CreateBookingWorkflowRequest, CancellationToken, Task<BookingConfirmationDto>> _createBooking;
    private readonly Func<string, DateTimeOffset, CancellationToken, Task<BookingConfirmationDto>> _rescheduleBooking;

    public List<CreateBookingWorkflowRequest> CreateRequests { get; } = [];

    public List<string> CancelledBookingIds { get; } = [];

    public List<(string BookingId, DateTimeOffset NewSlotStart)> RescheduleCalls { get; } = [];

    public StubBookingWorkflowService(
        Func<CancellationToken, Task<BookingOptionsDto>>? getOptions = null,
        Func<string, string, DateOnly, CancellationToken, Task<IReadOnlyList<WorkflowSlotDto>>>? getSlots = null,
        Func<CreateBookingWorkflowRequest, CancellationToken, Task<BookingConfirmationDto>>? createBooking = null,
        Func<string, DateTimeOffset, CancellationToken, Task<BookingConfirmationDto>>? rescheduleBooking = null)
    {
        _getOptions = getOptions ?? (_ => Task.FromResult(new BookingOptionsDto([], [], [])));
        _getSlots = getSlots ?? ((_, _, _, _) => Task.FromResult<IReadOnlyList<WorkflowSlotDto>>([]));
        _createBooking = createBooking ?? ((request, _) => Task.FromResult(new BookingConfirmationDto(
            "booking-new", request.CustomerName, request.ServiceName, request.SpecialistName, request.SlotStart, request.DurationMinutes, request.Price)));
        _rescheduleBooking = rescheduleBooking ?? ((bookingId, newSlotStart, _) => Task.FromResult(new BookingConfirmationDto(
            bookingId, "Test Customer", "Test Service", "Test Specialist", newSlotStart, 60, "$0")));
    }

    public Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default) =>
        _getOptions(cancellationToken);

    public Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
        _getSlots(specialistId, serviceId, scheduleDate, cancellationToken);

    public Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        return _createBooking(request, cancellationToken);
    }

    public Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        CancelledBookingIds.Add(bookingId);
        return Task.CompletedTask;
    }

    public Task<BookingConfirmationDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newSlotStart, CancellationToken cancellationToken = default)
    {
        RescheduleCalls.Add((bookingId, newSlotStart));
        return _rescheduleBooking(bookingId, newSlotStart, cancellationToken);
    }
}
