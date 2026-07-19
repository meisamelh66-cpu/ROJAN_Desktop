using Rojan.Desktop.Application.BookingWorkflow;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCalendar = Rojan.Desktop.Application.Calendar;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

public sealed class BookingWorkflowServiceTests
{
    private static readonly DateTimeOffset SlotStart = new(2026, 3, 2, 9, 0, 0, DateTimeOffset.Now.Offset);

    private static AppCustomers.CustomerDto MakeCustomer(string id, string name) =>
        new(id, name, string.Empty, string.Empty, string.Empty, AppCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty);

    private static AppServices.ServiceDto MakeService(string id, string name, AppServices.ServiceStatus status) =>
        new(id, name, AppServices.ServiceCategory.Hair, status, 60, "$65", string.Empty);

    private static AppSpecialists.SpecialistDto MakeSpecialist(string id, string name, AppSpecialists.SpecialistStatus status) =>
        new(id, name, string.Empty, string.Empty, string.Empty, status, string.Empty);

    private static BookingWorkflowService MakeSut(
        StubCustomerQueryService? customerQueryService = null,
        StubServiceQueryService? serviceQueryService = null,
        StubSpecialistQueryService? specialistQueryService = null,
        StubCalendarQueryService? calendarQueryService = null,
        StubCalendarCommandService? calendarCommandService = null,
        StubBookingQueryService? bookingQueryService = null,
        StubBookingCommandService? bookingCommandService = null) => new(
        customerQueryService ?? new StubCustomerQueryService([]),
        serviceQueryService ?? new StubServiceQueryService([]),
        specialistQueryService ?? new StubSpecialistQueryService([]),
        calendarQueryService ?? new StubCalendarQueryService(),
        calendarCommandService ?? new StubCalendarCommandService(),
        bookingQueryService ?? new StubBookingQueryService(),
        bookingCommandService ?? new StubBookingCommandService());

    [Fact]
    public async Task GetBookingOptionsAsync_FiltersServicesAndSpecialistsToActiveOnly()
    {
        var customerQueryService = new StubCustomerQueryService([MakeCustomer("customer-1", "Amelia Hart")]);
        var serviceQueryService = new StubServiceQueryService([
            MakeService("service-1", "Haircut & Style", AppServices.ServiceStatus.Active),
            MakeService("service-9", "Perm Styling", AppServices.ServiceStatus.Discontinued),
        ]);
        var specialistQueryService = new StubSpecialistQueryService([
            MakeSpecialist("specialist-1", "Jordan Lee", AppSpecialists.SpecialistStatus.Active),
            MakeSpecialist("specialist-5", "Sam Torres", AppSpecialists.SpecialistStatus.Inactive),
        ]);
        var sut = MakeSut(customerQueryService, serviceQueryService, specialistQueryService);

        var options = await sut.GetBookingOptionsAsync();

        Assert.Single(options.Customers);
        Assert.Single(options.Services);
        Assert.Equal("service-1", options.Services[0].Id);
        Assert.Single(options.Specialists);
        Assert.Equal("specialist-1", options.Specialists[0].Id);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_FiltersToAvailableOnly()
    {
        var slots = new List<AppCalendar.AvailabilitySlotDto>
        {
            new("specialist-1", "Jordan Lee", SlotStart, SlotStart.AddMinutes(30), AppCalendar.AvailabilityStatus.Available),
            new("specialist-1", "Jordan Lee", SlotStart.AddMinutes(30), SlotStart.AddMinutes(60), AppCalendar.AvailabilityStatus.Booked),
        };
        var calendarQueryService = new StubCalendarQueryService(
            getDailyAvailability: (specialistId, date, _) => Task.FromResult(
                new AppCalendar.DailyAvailabilityDto(specialistId, "Jordan Lee", date, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), slots)));
        var sut = MakeSut(calendarQueryService: calendarQueryService);

        var result = await sut.GetAvailableSlotsAsync("specialist-1", DateOnly.FromDateTime(SlotStart.Date));

        var slot = Assert.Single(result);
        Assert.Equal(SlotStart, slot.Start);
    }

    [Fact]
    public async Task CreateBookingAsync_ReservesSlotThenCreatesBooking()
    {
        var calendarCommandService = new StubCalendarCommandService();
        var bookingCommandService = new StubBookingCommandService();
        var sut = MakeSut(calendarCommandService: calendarCommandService, bookingCommandService: bookingCommandService);
        var request = new CreateBookingWorkflowRequest(
            "customer-1", "Amelia Hart", "service-1", "Haircut & Style", 60, "$65",
            "specialist-1", "Jordan Lee", SlotStart, string.Empty);

        var confirmation = await sut.CreateBookingAsync(request);

        Assert.Equal("booking-new", confirmation.BookingId);
        var reserveCall = Assert.Single(calendarCommandService.ReserveCalls);
        Assert.Equal("specialist-1", reserveCall.SpecialistId);
        Assert.Equal(SlotStart.AddMinutes(60), reserveCall.End);
        var createRequest = Assert.Single(bookingCommandService.CreateRequests);
        Assert.Equal("customer-1", createRequest.CustomerId);
        Assert.Empty(calendarCommandService.ReleaseCalls);
    }

    [Fact]
    public async Task CreateBookingAsync_BookingCreationFails_ReleasesReservedSlotAndRethrows()
    {
        var calendarCommandService = new StubCalendarCommandService();
        var bookingCommandService = new StubBookingCommandService { ThrowOnCreate = true };
        var sut = MakeSut(calendarCommandService: calendarCommandService, bookingCommandService: bookingCommandService);
        var request = new CreateBookingWorkflowRequest(
            "customer-1", "Amelia Hart", "service-1", "Haircut & Style", 60, "$65",
            "specialist-1", "Jordan Lee", SlotStart, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBookingAsync(request));

        Assert.Single(calendarCommandService.ReserveCalls);
        var releaseCall = Assert.Single(calendarCommandService.ReleaseCalls);
        Assert.Equal("specialist-1", releaseCall.SpecialistId);
    }

    [Fact]
    public async Task CreateBookingAsync_SlotAlreadyReserved_ThrowsAndNeverCreatesBooking()
    {
        var calendarCommandService = new StubCalendarCommandService { ThrowOnReserve = true };
        var bookingCommandService = new StubBookingCommandService();
        var sut = MakeSut(calendarCommandService: calendarCommandService, bookingCommandService: bookingCommandService);
        var request = new CreateBookingWorkflowRequest(
            "customer-1", "Amelia Hart", "service-1", "Haircut & Style", 60, "$65",
            "specialist-1", "Jordan Lee", SlotStart, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBookingAsync(request));

        Assert.Empty(bookingCommandService.CreateRequests);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingHasSpecialist_UpdatesStatusAndReleasesSlot()
    {
        var booking = new AppBookings.BookingDto(
            "booking-1", "customer-1", "Amelia Hart", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee",
            SlotStart, 60, "$65", AppBookings.BookingStatus.Confirmed, string.Empty);
        var bookingQueryService = new StubBookingQueryService([booking]);
        var bookingCommandService = new StubBookingCommandService();
        var calendarCommandService = new StubCalendarCommandService();
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService, calendarCommandService: calendarCommandService);

        await sut.CancelBookingAsync("booking-1");

        var updateCall = Assert.Single(bookingCommandService.UpdateStatusCalls);
        Assert.Equal(AppBookings.BookingStatus.Cancelled, updateCall.Status);
        var releaseCall = Assert.Single(calendarCommandService.ReleaseCalls);
        Assert.Equal("specialist-1", releaseCall.SpecialistId);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingHasNoSpecialist_UpdatesStatusButDoesNotReleaseSlot()
    {
        var booking = new AppBookings.BookingDto(
            "booking-3", string.Empty, "Olivia Chen", string.Empty, "Corporate Group Styling", string.Empty, "Priya Nair",
            SlotStart, 240, "$0", AppBookings.BookingStatus.Pending, string.Empty);
        var bookingQueryService = new StubBookingQueryService([booking]);
        var bookingCommandService = new StubBookingCommandService();
        var calendarCommandService = new StubCalendarCommandService();
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService, calendarCommandService: calendarCommandService);

        await sut.CancelBookingAsync("booking-3");

        Assert.Single(bookingCommandService.UpdateStatusCalls);
        Assert.Empty(calendarCommandService.ReleaseCalls);
    }

    [Fact]
    public async Task CancelBookingAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var sut = MakeSut(bookingQueryService: new StubBookingQueryService([]));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CancelBookingAsync("no-such-booking"));
    }
}
