using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Calendar;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Reserving/releasing a slot is part of the booking flow, so it requires <see cref="Permission.BookingEdit"/> - the same permission the Bookings module itself uses for status changes.</summary>
public sealed class CalendarCommandServicePermissionGate : ICalendarCommandService
{
    private readonly ICalendarCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public CalendarCommandServicePermissionGate(ICalendarCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task ReserveSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BookingEdit);
        return _inner.ReserveSlotAsync(specialistId, slotStart, slotEnd, cancellationToken);
    }

    public Task ReleaseSlotAsync(string specialistId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BookingEdit);
        return _inner.ReleaseSlotAsync(specialistId, slotStart, slotEnd, cancellationToken);
    }
}
