using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>
/// Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Only the write use cases (Create/Cancel/Reschedule) and <see cref="CreateGuestCustomerAsync"/> are gated - the wizard's own picker reads stay open to anyone who can reach the Bookings module.
///
/// Phase 3B Booking Salon-Scope Migration: the 3 booking write methods
/// (Create/Cancel/Reschedule) are migrated off the legacy
/// <see cref="IPermissionGate"/>/<c>RolePermissions</c> check entirely -
/// <see cref="IBackendPermissionGate"/>'s <c>MANAGE_BOOKINGS</c> check is
/// now their sole authority, not a parallel one. <c>MANAGE_OWN_BOOKINGS</c>/
/// Specialist scope is excluded, not silently folded in - see
/// <c>Bookings.BookingCommandServicePermissionGate</c>'s own doc comment
/// for the full reasoning. <see cref="CreateGuestCustomerAsync"/> is
/// unaffected - it is a Customers-scoped write (<see cref="Permission.CustomerEdit"/>),
/// out of this phase's scope, still gated by the legacy <see cref="IPermissionGate"/>,
/// which this class therefore still depends on for that one method alone.
/// </summary>
public sealed class BookingWorkflowServicePermissionGate : IBookingWorkflowService
{
    private const string ManageBookings = "MANAGE_BOOKINGS";

    private readonly IBookingWorkflowService _inner;
    private readonly IPermissionGate _permissionGate;
    private readonly IBackendPermissionGate _backendPermissionGate;

    public BookingWorkflowServicePermissionGate(IBookingWorkflowService inner, IPermissionGate permissionGate, IBackendPermissionGate backendPermissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
        _backendPermissionGate = backendPermissionGate;
    }

    public Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default) =>
        _inner.GetBookingOptionsAsync(cancellationToken);

    /// <summary>Reception Stabilization Sprint: gated on <see cref="Permission.CustomerEdit"/>, same permission <c>Customers.CustomerCommandServicePermissionGate</c> already requires for customer creation - this is a write (a new CRM customer record), unlike the picker reads above. Out of Phase 3B's Booking scope - untouched.</summary>
    public Task<WorkflowCustomerOptionDto> CreateGuestCustomerAsync(string fullName, string phone, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.CustomerEdit);
        return _inner.CreateGuestCustomerAsync(fullName, phone, cancellationToken);
    }

    public Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
        _inner.GetAvailableSlotsAsync(specialistId, serviceId, scheduleDate, cancellationToken);

    public Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.CreateBookingAsync(request, cancellationToken);
    }

    public Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.CancelBookingAsync(bookingId, cancellationToken);
    }

    public Task<BookingConfirmationDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newSlotStart, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageBookings);
        return _inner.RescheduleBookingAsync(bookingId, newSlotStart, cancellationToken);
    }
}
