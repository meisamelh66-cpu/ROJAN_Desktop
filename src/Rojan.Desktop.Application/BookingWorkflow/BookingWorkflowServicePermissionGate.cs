using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Only the two write use cases are gated (<see cref="Permission.BookingCreate"/>) - the wizard's own picker reads stay open to anyone who can reach the Bookings module.</summary>
public sealed class BookingWorkflowServicePermissionGate : IBookingWorkflowService
{
    private readonly IBookingWorkflowService _inner;
    private readonly IPermissionGate _permissionGate;

    public BookingWorkflowServicePermissionGate(IBookingWorkflowService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default) =>
        _inner.GetBookingOptionsAsync(cancellationToken);

    public Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default) =>
        _inner.GetAvailableSlotsAsync(specialistId, scheduleDate, cancellationToken);

    public Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BookingCreate);
        return _inner.CreateBookingAsync(request, cancellationToken);
    }

    public Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BookingCreate);
        return _inner.CancelBookingAsync(bookingId, cancellationToken);
    }

    public Task<BookingConfirmationDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newSlotStart, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BookingCreate);
        return _inner.RescheduleBookingAsync(bookingId, newSlotStart, cancellationToken);
    }
}
