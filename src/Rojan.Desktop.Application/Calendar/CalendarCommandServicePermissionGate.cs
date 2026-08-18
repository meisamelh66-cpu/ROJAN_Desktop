using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Reserving/releasing a slot is part of the booking flow, so it required <see cref="Permission.BookingEdit"/> historically - the same permission the Bookings module itself used for status changes.
///
/// Phase 3B Booking Salon-Scope Migration: migrated off the legacy
/// <see cref="IPermissionGate"/>/<c>RolePermissions</c> check entirely -
/// same reasoning as <c>Bookings.BookingCommandServicePermissionGate</c>'s
/// own doc comment, identical here. <c>MANAGE_OWN_BOOKINGS</c>/Specialist
/// scope is likewise excluded, not silently folded in.
/// </summary>
public sealed class CalendarCommandServicePermissionGate : ICalendarCommandService
{
    private const string ManageBookings = "MANAGE_BOOKINGS";

    private readonly ICalendarCommandService _inner;
    private readonly IBackendPermissionGate _backendPermissionGate;

    public CalendarCommandServicePermissionGate(ICalendarCommandService inner, IBackendPermissionGate backendPermissionGate)
    {
        _inner = inner;
        _backendPermissionGate = backendPermissionGate;
    }

    public Task ReserveSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.ReserveSlotAsync(specialistId, slotStart, slotEnd, cancellationToken);
    }

    public Task ReleaseSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.ReleaseSlotAsync(specialistId, slotStart, slotEnd, cancellationToken);
    }
}
