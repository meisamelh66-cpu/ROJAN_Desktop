using Rojan.Desktop.Application.Organizations;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Default <see cref="IBookingQueryService"/> implementation - fetches
/// from <see cref="DomainBookings.IBookingRepository"/> (Application is
/// allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="BookingMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
///
/// Phase 22A: scoped to <see cref="IEnterpriseContext"/> - the "Appointments"
/// module's Organization/Branch Scoping guarantee, same reasoning as
/// <c>Customers.CustomerQueryService</c>.
/// </summary>
public sealed class BookingQueryService : IBookingQueryService
{
    private readonly DomainBookings.IBookingRepository _repository;
    private readonly IEnterpriseContext _enterpriseContext;

    public BookingQueryService(DomainBookings.IBookingRepository repository, IEnterpriseContext enterpriseContext)
    {
        _repository = repository;
        _enterpriseContext = enterpriseContext;
    }

    public async Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        var bookings = await _repository.GetBookingsAsync(cancellationToken).ConfigureAwait(true);
        return ScopeToCurrentSession(bookings).Select(BookingMapper.MapBooking).ToList();
    }

    public async Task<BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId, cancellationToken).ConfigureAwait(true);
        return booking is null || !IsInCurrentSession(booking) ? null : BookingMapper.MapBooking(booking);
    }

    /// <summary>
    /// Composes over <see cref="DomainBookings.IBookingRepository.GetBookingsAsync"/>
    /// rather than a dedicated repository search method - search/filtering
    /// is a read-composition concern Application owns, not a new Domain
    /// contract, same reasoning as <c>Customers.CustomerQueryService.SearchCustomersAsync</c>.
    /// Every criterion is applied only when present, so an all-default
    /// filter falls through to exactly the same pipeline as
    /// <see cref="GetBookingsAsync"/>.
    /// </summary>
    public async Task<IReadOnlyList<BookingDto>> SearchBookingsAsync(BookingSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var bookings = ScopeToCurrentSession(await _repository.GetBookingsAsync(cancellationToken).ConfigureAwait(true));

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            bookings = bookings.Where(booking =>
                booking.CustomerName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                booking.ServiceName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                booking.SpecialistName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                booking.Notes.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
        {
            bookings = bookings.Where(booking => booking.CustomerName.Contains(filter.CustomerName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
        {
            bookings = bookings.Where(booking => booking.ServiceName.Contains(filter.ServiceName, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Status.HasValue)
        {
            var domainStatus = BookingMapper.MapStatusToDomain(filter.Status.Value);
            bookings = bookings.Where(booking => booking.Status == domainStatus);
        }

        if (filter.DateFrom.HasValue)
        {
            bookings = bookings.Where(booking => DateOnly.FromDateTime(booking.ScheduledAt.DateTime) >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            bookings = bookings.Where(booking => DateOnly.FromDateTime(booking.ScheduledAt.DateTime) <= filter.DateTo.Value);
        }

        return bookings.Select(BookingMapper.MapBooking).ToList();
    }

    private IEnumerable<DomainBookings.Booking> ScopeToCurrentSession(IEnumerable<DomainBookings.Booking> bookings) =>
        bookings.Where(IsInCurrentSession);

    private bool IsInCurrentSession(DomainBookings.Booking booking) =>
        booking.OrganizationId == _enterpriseContext.CurrentOrganizationId &&
        (_enterpriseContext.CurrentBranchId is null || booking.BranchId == _enterpriseContext.CurrentBranchId);
}
