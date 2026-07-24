using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Minimal, list-backed <see cref="IBookingQueryService"/> test double - needed at the Application-service level (distinct from Bookings.StubBookingRepository, which is a Domain-repository double) because <see cref="Rojan.Desktop.Application.BookingWorkflow.BookingWorkflowService"/> depends on the service interface, not the repository.</summary>
internal sealed class StubBookingQueryService : IBookingQueryService
{
    private readonly List<BookingDto> _bookings;

    public StubBookingQueryService(IReadOnlyList<BookingDto>? bookings = null)
    {
        _bookings = bookings?.ToList() ?? [];
    }

    public Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BookingDto>>(_bookings.ToList());

    public Task<BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_bookings.FirstOrDefault(booking => booking.Id == bookingId));

    public Task<IReadOnlyList<BookingDto>> SearchBookingsAsync(BookingSearchFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by BookingWorkflowService.");
}
