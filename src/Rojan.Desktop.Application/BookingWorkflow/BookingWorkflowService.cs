using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCalendar = Rojan.Desktop.Application.Calendar;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>Default <see cref="IBookingWorkflowService"/> implementation - see the interface doc comment for why depending on five sibling Application services here is expected, not a layering violation.</summary>
public sealed class BookingWorkflowService : IBookingWorkflowService
{
    private readonly AppCustomers.ICustomerQueryService _customerQueryService;
    private readonly AppServices.IServiceQueryService _serviceQueryService;
    private readonly AppSpecialists.ISpecialistQueryService _specialistQueryService;
    private readonly AppCalendar.ICalendarQueryService _calendarQueryService;
    private readonly AppCalendar.ICalendarCommandService _calendarCommandService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly AppBookings.IBookingCommandService _bookingCommandService;

    public BookingWorkflowService(
        AppCustomers.ICustomerQueryService customerQueryService,
        AppServices.IServiceQueryService serviceQueryService,
        AppSpecialists.ISpecialistQueryService specialistQueryService,
        AppCalendar.ICalendarQueryService calendarQueryService,
        AppCalendar.ICalendarCommandService calendarCommandService,
        AppBookings.IBookingQueryService bookingQueryService,
        AppBookings.IBookingCommandService bookingCommandService)
    {
        _customerQueryService = customerQueryService;
        _serviceQueryService = serviceQueryService;
        _specialistQueryService = specialistQueryService;
        _calendarQueryService = calendarQueryService;
        _calendarCommandService = calendarCommandService;
        _bookingQueryService = bookingQueryService;
        _bookingCommandService = bookingCommandService;
    }

    public async Task<BookingOptionsDto> GetBookingOptionsAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(true);
        var services = await _serviceQueryService.GetServicesAsync(cancellationToken).ConfigureAwait(true);
        var specialists = await _specialistQueryService.GetSpecialistsAsync(cancellationToken).ConfigureAwait(true);

        var customerOptions = customers
            .Select(customer => new WorkflowCustomerOptionDto(customer.Id, customer.FullName))
            .ToList();
        var serviceOptions = services
            .Where(service => service.Status == AppServices.ServiceStatus.Active)
            .Select(service => new WorkflowServiceOptionDto(service.Id, service.Name, service.DurationMinutes, service.Price))
            .ToList();
        var specialistOptions = specialists
            .Where(specialist => specialist.Status == AppSpecialists.SpecialistStatus.Active)
            .Select(specialist => new WorkflowSpecialistOptionDto(specialist.Id, specialist.FullName))
            .ToList();

        return new BookingOptionsDto(customerOptions, serviceOptions, specialistOptions);
    }

    public async Task<IReadOnlyList<WorkflowSlotDto>> GetAvailableSlotsAsync(string specialistId, DateOnly scheduleDate, CancellationToken cancellationToken = default)
    {
        var availability = await _calendarQueryService.GetDailyAvailabilityAsync(specialistId, scheduleDate, cancellationToken).ConfigureAwait(true);
        return availability.Slots
            .Where(slot => slot.Status == AppCalendar.AvailabilityStatus.Available)
            .Select(slot => new WorkflowSlotDto(slot.Start, slot.End))
            .ToList();
    }

    public async Task<BookingConfirmationDto> CreateBookingAsync(CreateBookingWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var slotEnd = request.SlotStart.AddMinutes(request.DurationMinutes);

        // Reserve the calendar slot first - ReserveSlotAsync re-checks for
        // a conflict at write time, so this is the operation most likely
        // to fail; creating the booking second means a failed reservation
        // never leaves an orphaned booking behind.
        await _calendarCommandService.ReserveSlotAsync(request.SpecialistId, request.SlotStart, slotEnd, cancellationToken).ConfigureAwait(true);

        try
        {
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
        catch
        {
            // No database transaction spans the calendar reservation and
            // the booking write, so a failed booking creation must release
            // the slot it just reserved by hand - otherwise the slot would
            // be stuck Booked with nothing booked against it.
            await _calendarCommandService.ReleaseSlotAsync(request.SpecialistId, request.SlotStart, slotEnd, cancellationToken).ConfigureAwait(true);
            throw;
        }
    }

    public async Task CancelBookingAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingQueryService.GetBookingByIdAsync(bookingId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        await _bookingCommandService.UpdateBookingStatusAsync(bookingId, AppBookings.BookingStatus.Cancelled, cancellationToken).ConfigureAwait(true);

        if (!string.IsNullOrEmpty(booking.SpecialistId))
        {
            var slotEnd = booking.ScheduledAt.AddMinutes(booking.DurationMinutes);
            await _calendarCommandService.ReleaseSlotAsync(booking.SpecialistId, booking.ScheduledAt, slotEnd, cancellationToken).ConfigureAwait(true);
        }
    }
}
