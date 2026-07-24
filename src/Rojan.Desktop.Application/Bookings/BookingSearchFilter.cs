namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Combinable filter criteria for <see cref="IBookingQueryService.SearchBookingsAsync"/> -
/// every field is optional and ANDed together when present. A filter with
/// every field left at its default is equivalent to no filtering at all:
/// <see cref="BookingQueryService.SearchBookingsAsync"/> returns the exact
/// same result set, in the same order, as <see cref="IBookingQueryService.GetBookingsAsync"/>
/// in that case - "no filter applied" behaves identically to before this
/// filter existed.
/// </summary>
public sealed record BookingSearchFilter(
    string? SearchText = null,
    string? CustomerName = null,
    string? ServiceName = null,
    BookingStatus? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null)
{
    public static BookingSearchFilter Empty { get; } = new();
}
