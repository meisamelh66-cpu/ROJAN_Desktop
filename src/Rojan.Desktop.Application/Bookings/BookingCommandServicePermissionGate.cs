using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>.</summary>
public sealed class BookingCommandServicePermissionGate : IBookingCommandService
{
    private readonly IBookingCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public BookingCommandServicePermissionGate(IBookingCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BookingCreate);
        return _inner.CreateBookingAsync(request, cancellationToken);
    }

    public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BookingEdit);
        return _inner.UpdateBookingStatusAsync(bookingId, status, cancellationToken);
    }
}
