using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Call-recording <see cref="IBookingCommandService"/> test double, with a switch to make <see cref="CreateBookingAsync"/> throw - needed to exercise <see cref="Rojan.Desktop.Application.BookingWorkflow.BookingWorkflowService.CreateBookingAsync"/>'s calendar-reservation rollback path.</summary>
internal sealed class StubBookingCommandService : IBookingCommandService
{
    public List<CreateBookingRequest> CreateRequests { get; } = [];

    public List<(string BookingId, BookingStatus Status)> UpdateStatusCalls { get; } = [];

    public bool ThrowOnCreate { get; set; }

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
            request.Price, BookingStatus.Pending, request.Notes));
    }

    public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        UpdateStatusCalls.Add((bookingId, status));
        return Task.FromResult(new BookingDto(
            bookingId, string.Empty, "Test Customer", string.Empty, "Test Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", status, string.Empty));
    }
}
