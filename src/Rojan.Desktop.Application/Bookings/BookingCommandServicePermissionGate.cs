using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>.
///
/// Phase 3B Booking Salon-Scope Migration: migrated off the legacy
/// <see cref="IPermissionGate"/>/<c>RolePermissions</c> check entirely -
/// <see cref="IBackendPermissionGate"/>'s <c>MANAGE_BOOKINGS</c> check is
/// now this class's sole authority, not a parallel/dual one (per
/// ROJAN_Phase3B_Booking_SalonScope_Migration_Report_v1.md). Scoped to
/// <c>SALON_OWNER</c>/<c>RECEPTIONIST</c> only - <c>MANAGE_OWN_BOOKINGS</c>
/// (the Specialist case) is deliberately excluded from this check, not
/// silently folded in: the backend's own <c>SalonPermissionResolver</c>
/// never grants a Specialist link <c>MANAGE_BOOKINGS</c> or
/// <c>MANAGE_OWN_BOOKINGS</c> (only <c>MANAGE_SCHEDULE_OWN</c>, live-
/// verified), so a Specialist session is correctly, uniformly denied here
/// until that scope is deliberately migrated - not a workaround, a
/// temporary exclusion stated as one.
/// </summary>
public sealed class BookingCommandServicePermissionGate : IBookingCommandService
{
    private const string ManageBookings = "MANAGE_BOOKINGS";

    private readonly IBookingCommandService _inner;
    private readonly IBackendPermissionGate _backendPermissionGate;

    public BookingCommandServicePermissionGate(IBookingCommandService inner, IBackendPermissionGate backendPermissionGate)
    {
        _inner = inner;
        _backendPermissionGate = backendPermissionGate;
    }

    public bool SupportsInProgressAndNoShowStatuses => _inner.SupportsInProgressAndNoShowStatuses;

    public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.CreateBookingAsync(request, cancellationToken);
    }

    public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.UpdateBookingStatusAsync(bookingId, status, cancellationToken);
    }

    public Task<BookingDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.RescheduleBookingAsync(bookingId, newScheduledAt, cancellationToken);
    }
}
