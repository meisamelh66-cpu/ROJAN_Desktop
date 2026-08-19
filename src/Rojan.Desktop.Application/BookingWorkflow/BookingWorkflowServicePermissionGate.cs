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
/// a Customers-scoped write, out of Phase 3B's Booking scope but squarely
/// in Phase 3C's Customers scope.
///
/// Phase 3C Customers CRM Permission Migration: <see cref="CreateGuestCustomerAsync"/>
/// required the backend's <c>MANAGE_CRM</c> permission in addition to legacy
/// <see cref="Permission.CustomerEdit"/>, the identical dual check
/// <c>Customers.CustomerCommandServicePermissionGate</c> performs for every
/// other Customer write - superseded below.
///
/// Reception Permission Contract Alignment: <see cref="CreateGuestCustomerAsync"/> now checks
/// the backend for <c>CREATE_CUSTOMER_IDENTITY</c> <em>or</em> <c>MANAGE_CRM</c>
/// (<see cref="IBackendPermissionGate.EnsureBackendAny"/>), not <c>MANAGE_CRM</c> alone. This
/// is what actually unblocks Reception here - the backend's real <c>RECEPTIONIST</c> role never
/// grants <c>MANAGE_CRM</c> (see this class's own git history for the live-verified mismatch
/// that motivated this change), but does grant <c>CREATE_CUSTOMER_IDENTITY</c>, resolved by a
/// structurally narrower backend path (<c>ROJAN_Reception_Permission_Contract_Update_ADR_v1.md</c>).
/// Legacy <see cref="Permission.CustomerEdit"/> stays as the first check, unchanged - this phase
/// only widens the backend half of the dual gate.
/// </summary>
public sealed class BookingWorkflowServicePermissionGate : IBookingWorkflowService
{
    private const string ManageBookings = "MANAGE_BOOKINGS";
    private const string ManageCrm = "MANAGE_CRM";
    private const string CreateCustomerIdentity = "CREATE_CUSTOMER_IDENTITY";

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

    /// <summary>Reception Stabilization Sprint: gated on <see cref="Permission.CustomerEdit"/>, same permission <c>Customers.CustomerCommandServicePermissionGate</c> already requires for customer creation - this is a write (a new customer identity record), unlike the picker reads above. Reception Permission Contract Alignment: additionally requires the backend's <c>CREATE_CUSTOMER_IDENTITY</c> or <c>MANAGE_CRM</c> - see this class's own doc comment.</summary>
    public Task<WorkflowCustomerOptionDto> CreateGuestCustomerAsync(string fullName, string phone, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.CustomerEdit);
        _backendPermissionGate.EnsureBackendAny(CreateCustomerIdentity, ManageCrm);
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
