using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Call-recording <see cref="IBookingCommandService"/> test double, with switches to make <see cref="CreateBookingAsync"/>/<see cref="RescheduleBookingAsync"/> throw - needed to exercise <see cref="Rojan.Desktop.Application.BookingWorkflow.BookingWorkflowService"/>'s calendar-reservation rollback paths.</summary>
internal sealed class StubBookingCommandService : IBookingCommandService
{
    public List<CreateBookingRequest> CreateRequests { get; } = [];

    public List<(string BookingId, BookingStatus Status)> UpdateStatusCalls { get; } = [];

    public List<(string BookingId, DateTimeOffset NewScheduledAt)> RescheduleCalls { get; } = [];

    public bool ThrowOnCreate { get; set; }

    public bool ThrowOnReschedule { get; set; }

    public bool SupportsInProgressAndNoShowStatuses => true;

    public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCreate)
        {
            throw new InvalidOperationException("Booking could not be created.");
        }

        CreateRequests.Add(request);
        return Task.FromResult(new BookingDto(
            "booking-new", request.CustomerId, request.CustomerName, request.ServiceId, request.ServiceName,
            request.SpecialistId, request.SpecialistName, request.ScheduledAt, request.DurationMinutes,
            request.Price, BookingStatus.Pending, request.Notes, "org-1", "branch-1"));
    }

    public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        UpdateStatusCalls.Add((bookingId, status));
        return Task.FromResult(new BookingDto(
            bookingId, string.Empty, "Test Customer", string.Empty, "Test Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", status, string.Empty, "org-1", "branch-1"));
    }

    public Task<BookingDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default)
    {
        if (ThrowOnReschedule)
        {
            throw new InvalidOperationException("Booking could not be rescheduled.");
        }

        RescheduleCalls.Add((bookingId, newScheduledAt));
        return Task.FromResult(new BookingDto(
            bookingId, string.Empty, "Test Customer", string.Empty, "Test Service", "specialist-1", "Jordan Lee",
            newScheduledAt, 60, "$0", BookingStatus.Confirmed, string.Empty, "org-1", "branch-1"));
    }
}
