using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Presentation.Tests.Bookings;

/// <summary>Configurable <see cref="IBookingQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubBookingQueryService : IBookingQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<BookingDto>>> _getBookings;
    private readonly Func<BookingSearchFilter, CancellationToken, Task<IReadOnlyList<BookingDto>>> _searchBookings;

    /// <summary>Every filter this stub was asked to search with, in call order - lets ViewModel tests assert on the composed <see cref="BookingSearchFilter"/> without needing a real filtering implementation here.</summary>
    public List<BookingSearchFilter> SearchCalls { get; } = [];

    public StubBookingQueryService(
        Func<CancellationToken, Task<IReadOnlyList<BookingDto>>> getBookings,
        Func<BookingSearchFilter, CancellationToken, Task<IReadOnlyList<BookingDto>>>? searchBookings = null)
    {
        _getBookings = getBookings;

        // Defaults to ignoring the filter and returning the same fixed list
        // GetBookingsAsync would - every existing test that only supplies
        // getBookings keeps working unchanged now that BookingPageViewModel
        // calls SearchBookingsAsync instead of GetBookingsAsync for every
        // load (Sprint 3 Commit 2).
        _searchBookings = searchBookings ?? ((_, cancellationToken) => _getBookings(cancellationToken));
    }

    public Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        _getBookings(cancellationToken);

    public async Task<BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        var bookings = await _getBookings(cancellationToken).ConfigureAwait(true);
        return bookings.FirstOrDefault(booking => booking.Id == bookingId);
    }

    public Task<IReadOnlyList<BookingDto>> SearchBookingsAsync(BookingSearchFilter filter, CancellationToken cancellationToken = default)
    {
        SearchCalls.Add(filter);
        return _searchBookings(filter, cancellationToken);
    }
}
