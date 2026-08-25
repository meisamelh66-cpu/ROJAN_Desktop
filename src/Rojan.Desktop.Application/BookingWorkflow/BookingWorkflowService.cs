using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCalendar = Rojan.Desktop.Application.Calendar;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>Default <see cref="IBookingWorkflowService"/> implementation - see the interface doc comment for why depending on six sibling Application services here is expected, not a layering violation (Reception Stabilization Sprint added <see cref="AppCustomers.ICustomerIdentityService"/>, for <see cref="CreateGuestCustomerAsync"/> - Reception Permission Contract Alignment: rewired off <see cref="AppCustomers.ICustomerCommandService"/> onto this narrower, booking-time-only service).</summary>
public sealed class BookingWorkflowService : IBookingWorkflowService
{
    private readonly AppCustomers.ICustomerQueryService _customerQueryService;
    private readonly AppServices.IServiceQueryService _serviceQueryService;
    private readonly AppSpecialists.ISpecialistQueryService _specialistQueryService;
    private readonly AppCalendar.ICalendarQueryService _calendarQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly AppBookings.IBookingCommandService _bookingCommandService;
    private readonly AppCustomers.ICustomerIdentityService _customerIdentityService;

    public BookingWorkflowService(
        AppCustomers.ICustomerQueryService customerQueryService,
        AppServices.IServiceQueryService serviceQueryService,
        AppSpecialists.ISpecialistQueryService specialistQueryService,
        AppCalendar.ICalendarQueryService calendarQueryService,
        AppBookings.IBookingQueryService bookingQueryService,
        AppBookings.IBookingCommandService bookingCommandService,
        AppCustomers.ICustomerIdentityService customerIdentityService)
    {
        _customerQueryService = customerQueryService;
        _serviceQueryService = serviceQueryService;
        _specialistQueryService = specialistQueryService;
        _calendarQueryService = calendarQueryService;
        _bookingQueryService = bookingQueryService;
        _bookingCommandService = bookingCommandService;
        _customerIdentityService = customerIdentityService;
    }

    public async Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(true);
        var services = await _serviceQueryService.GetServicesAsync(cancellationToken).ConfigureAwait(true);
        var specialists = await _specialistQueryService.GetSpecialistsAsync(cancellationToken).ConfigureAwait(true);

        var customerOptions = customers
            .Select(customer => new WorkflowCustomerOptionDto(customer.Id, customer.FullName, customer.UserId is not null))
            .ToList();
        var serviceOptions = services
            .Where(service => service.Status == AppServices.ServiceStatus.Active)
            .Select(service => new WorkflowServiceOptionDto(service.Id, service.Name, service.DurationMinutes, service.Price))
            .ToList();
        var specialistOptions = new List<WorkflowSpecialistOptionDto>();
        foreach (var specialist in specialists.Where(specialist => specialist.Status == AppSpecialists.SpecialistStatus.Active))
        {
            // Booking Eligibility Filter: same per-specialist fan-out shape
            // Specialists.SpecialistQueryService.SearchSpecialistsAsync(SpecialistSearchFilter)
            // already uses for its own Skill filter - not a new technique.
            var assignedServiceIds = await _specialistQueryService.GetAssignedServiceIdsAsync(specialist.Id, cancellationToken).ConfigureAwait(true);
            specialistOptions.Add(new WorkflowSpecialistOptionDto(specialist.Id, specialist.FullName, assignedServiceIds));
        }

        return new BookingOptionsDto(customerOptions, serviceOptions, specialistOptions);
    }

    public async Task<WorkflowCustomerOptionDto> CreateGuestCustomerAsync(string fullName, string phone, CancellationToken cancellationToken = default)
    {
        var created = await _customerIdentityService.CreateCustomerIdentityAsync(fullName, phone, email: null, cancellationToken).ConfigureAwait(true);
        return new WorkflowCustomerOptionDto(created.Id, created.FullName, IsLinkedToAccount: false);
    }

    public async Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, string serviceId, DateOnly scheduleDate, CancellationToken cancellationToken = default)
    {
        var availability = await _calendarQueryService.GetDailyAvailabilityAsync(specialistId, serviceId, scheduleDate, cancellationToken).ConfigureAwait(true);
        return availability.Slots
            .Where(slot => slot.Status == AppCalendar.AvailabilityStatus.Available)
            .Select(slot => new WorkflowSlotDto(slot.Start, slot.End))
            .ToList();
    }

    public async Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        // Governance correction (ROJAN Architecture Governance V1.0 / ADR-004): this used to
        // reserve a real Calendar slot here before writing the booking, re-checking for a
        // conflict client-side. Backend is the only Booking Authority - its own advisory-lock
        // conflict check inside the create endpoint is the sole place that decision is made now.
        var createRequest = new AppBookings.CreateBookingRequest(
            request.CustomerName,
            request.ServiceName,
            request.SpecialistName,
            request.SlotStart,
            request.DurationMinutes,
            request.Notes,
            request.CustomerId,
            request.ServiceId,
            request.SpecialistId,
            request.Price);

        var booking = await _bookingCommandService.CreateBookingAsync(createRequest, cancellationToken).ConfigureAwait(true);

        return new BookingConfirmationDto(
            booking.Id,
            booking.CustomerName,
            booking.ServiceName,
            booking.SpecialistName,
            booking.ScheduledAt,
            booking.DurationMinutes,
            booking.Price);
    }

    public async Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        _ = await _bookingQueryService.GetBookingByIdAsync(bookingId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        // Governance correction: no Calendar slot to release here anymore - see CreateBookingAsync's
        // comment. The existence check above is unrelated to that removal and stays as-is.
        await _bookingCommandService.UpdateBookingStatusAsync(bookingId, AppBookings.BookingStatus.Cancelled, cancellationToken).ConfigureAwait(true);
    }

    public async Task<BookingConfirmationDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newSlotStart, CancellationToken cancellationToken = default)
    {
        _ = await _bookingQueryService.GetBookingByIdAsync(bookingId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        // Governance correction: no Calendar reserve/release orchestration here anymore - see
        // CreateBookingAsync's comment. BookingCommandService.RescheduleBookingAsync still enforces
        // eligibility; Backend's own conflict check is the sole authority on whether the new time
        // is available. The existence check above is unrelated to that removal and stays as-is.
        var moved = await _bookingCommandService.RescheduleBookingAsync(bookingId, newSlotStart, cancellationToken).ConfigureAwait(true);
        return ToConfirmation(moved);
    }

    private static BookingConfirmationDto ToConfirmation(AppBookings.BookingDto booking) => new(
        booking.Id, booking.CustomerName, booking.ServiceName, booking.SpecialistName, booking.ScheduledAt, booking.DurationMinutes, booking.Price);
}
