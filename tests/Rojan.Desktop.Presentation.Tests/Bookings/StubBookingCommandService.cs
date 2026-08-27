using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Presentation.Tests.Bookings;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubBookingCommandService : IBookingCommandService
{
    public List<CreateBookingRequest> CreateRequests { get; } = [];

    public List<(string BookingId, BookingStatus Status)> UpdateStatusCalls { get; } = [];

    /// <summary>Optional hook run after a booking is created, before the DTO is returned - lets a test mirror the created booking into whatever backing list the paired query-service stub reads from.</summary>
    public Action<CreateBookingRequest, BookingDto>? OnBookingCreated { get; set; }

    public bool SupportsInProgressAndNoShowStatuses { get; set; } = true;

    /// <summary>Phase 7.4.4 Booking/Checkout Error Hardening: when set, <see cref="CreateBookingAsync"/> throws this instead of succeeding - lets a test exercise the new try/catch without a real backend failure.</summary>
    public Exception? CreateFailure { get; set; }

    /// <summary>Same as <see cref="CreateFailure"/>, for <see cref="UpdateBookingStatusAsync"/>.</summary>
    public Exception? UpdateStatusFailure { get; set; }

    public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (CreateFailure is not null)
        {
            return Task.FromException<BookingDto>(CreateFailure);
        }

        CreateRequests.Add(request);
        var dto = new BookingDto(
            "new-booking", request.CustomerId, request.CustomerName, request.ServiceId, request.ServiceName,
            request.SpecialistId, request.SpecialistName, request.ScheduledAt, request.DurationMinutes,
            request.Price, BookingStatus.Pending, request.Notes, "org-1", "branch-1");
        OnBookingCreated?.Invoke(request, dto);
        return Task.FromResult(dto);
    }

    public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        if (UpdateStatusFailure is not null)
        {
            return Task.FromException<BookingDto>(UpdateStatusFailure);
        }

        UpdateStatusCalls.Add((bookingId, status));
        return Task.FromResult(new BookingDto(
            bookingId, string.Empty, "Test Customer", string.Empty, "Test Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", status, string.Empty, "org-1", "branch-1"));
    }

    /// <summary>Not exercised by BookingPageViewModelTests - reschedule goes through IBookingWorkflowService, never this plain command service directly (see StubBookingWorkflowService.RescheduleCalls instead).</summary>
    public Task<BookingDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by BookingPageViewModelTests - reschedule goes through IBookingWorkflowService.");
}
