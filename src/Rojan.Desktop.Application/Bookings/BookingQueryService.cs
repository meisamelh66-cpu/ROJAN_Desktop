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

    private IEnumerable<DomainBookings.Booking> ScopeToCurrentSession(IEnumerable<DomainBookings.Booking> bookings) =>
        bookings.Where(IsInCurrentSession);

    private bool IsInCurrentSession(DomainBookings.Booking booking) =>
        booking.OrganizationId == _enterpriseContext.CurrentOrganizationId &&
        (_enterpriseContext.CurrentBranchId is null || booking.BranchId == _enterpriseContext.CurrentBranchId);
}
