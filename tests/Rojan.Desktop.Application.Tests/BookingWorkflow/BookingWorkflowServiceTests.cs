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

    private static AppCustomers.CustomerDto MakeCustomer(string id, string name, string? userId = null) =>
        new(id, name, string.Empty, string.Empty, string.Empty, AppCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1", userId);

    private static AppServices.ServiceDto MakeService(string id, string name, AppServices.ServiceStatus status) =>
        new(id, name, AppServices.ServiceCategory.Hair, status, 60, "$65", string.Empty);

    private static AppSpecialists.SpecialistDto MakeSpecialist(string id, string name, AppSpecialists.SpecialistStatus status) =>
        new(id, name, string.Empty, string.Empty, string.Empty, status, string.Empty);

    private static BookingWorkflowService MakeSut(
        StubCustomerQueryService? customerQueryService = null,
        StubServiceQueryService? serviceQueryService = null,
        StubSpecialistQueryService? specialistQueryService = null,
        StubCalendarQueryService? calendarQueryService = null,
        StubBookingQueryService? bookingQueryService = null,
        StubBookingCommandService? bookingCommandService = null,
        StubCustomerIdentityService? customerIdentityService = null) => new(
        customerQueryService ?? new StubCustomerQueryService([]),
        serviceQueryService ?? new StubServiceQueryService([]),
        specialistQueryService ?? new StubSpecialistQueryService([]),
        calendarQueryService ?? new StubCalendarQueryService(),
        bookingQueryService ?? new StubBookingQueryService(),
        bookingCommandService ?? new StubBookingCommandService(),
        customerIdentityService ?? new StubCustomerIdentityService());

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
            getDailyAvailability: (specialistId, _, date, _) => Task.FromResult(
                new AppCalendar.DailyAvailabilityDto(specialistId, "Jordan Lee", date, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), slots)));
        var sut = MakeSut(calendarQueryService: calendarQueryService);

        var result = await sut.GetAvailableSlotsAsync("specialist-1", "service-1", DateOnly.FromDateTime(SlotStart.Date));

        var slot = Assert.Single(result);
        Assert.Equal(SlotStart, slot.Start);
    }

    // Governance correction (ROJAN Architecture Governance V1.0 / ADR-004): this class used to
    // reserve/release a real Calendar slot around every write here, and re-check for a conflict
    // client-side before creating/rescheduling a booking. Backend is the only Booking Authority -
    // that orchestration is removed, not demoted to advisory. These tests now cover only what this
    // service still does: forward to IBookingCommandService and propagate whatever it returns/throws.

    [Fact]
    public async Task CreateBookingAsync_CreatesBooking()
    {
        var bookingCommandService = new StubBookingCommandService();
        var sut = MakeSut(bookingCommandService: bookingCommandService);
        var request = new CreateBookingWorkflowRequest(
            "customer-1", "Amelia Hart", "service-1", "Haircut & Style", 60, "$65",
            "specialist-1", "Jordan Lee", SlotStart, string.Empty);

        var confirmation = await sut.CreateBookingAsync(request);

        Assert.Equal("booking-new", confirmation.BookingId);
        var createRequest = Assert.Single(bookingCommandService.CreateRequests);
        Assert.Equal("customer-1", createRequest.CustomerId);
    }

    [Fact]
    public async Task CreateBookingAsync_BookingCommandServiceThrows_Rethrows()
    {
        var bookingCommandService = new StubBookingCommandService { ThrowOnCreate = true };
        var sut = MakeSut(bookingCommandService: bookingCommandService);
        var request = new CreateBookingWorkflowRequest(
            "customer-1", "Amelia Hart", "service-1", "Haircut & Style", 60, "$65",
            "specialist-1", "Jordan Lee", SlotStart, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CancelBookingAsync_UpdatesStatusToCancelled()
    {
        var booking = new AppBookings.BookingDto(
            "booking-1", "customer-1", "Amelia Hart", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee",
            SlotStart, 60, "$65", AppBookings.BookingStatus.Confirmed, string.Empty, "org-1", "branch-1");
        var bookingQueryService = new StubBookingQueryService([booking]);
        var bookingCommandService = new StubBookingCommandService();
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService);

        await sut.CancelBookingAsync("booking-1");

        var updateCall = Assert.Single(bookingCommandService.UpdateStatusCalls);
        Assert.Equal(AppBookings.BookingStatus.Cancelled, updateCall.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var sut = MakeSut(bookingQueryService: new StubBookingQueryService([]));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CancelBookingAsync("no-such-booking"));
    }

    private static AppBookings.BookingDto MakeScheduledBooking(DateTimeOffset scheduledAt, string specialistId = "specialist-1") =>
        new("booking-1", "customer-1", "Amelia Hart", "service-1", "Haircut & Style", specialistId, "Jordan Lee",
            scheduledAt, 60, "$65", AppBookings.BookingStatus.Confirmed, string.Empty, "org-1", "branch-1");

    [Fact]
    public async Task RescheduleBookingAsync_UpdatesScheduledAt()
    {
        var newStart = SlotStart.AddDays(1);
        var bookingQueryService = new StubBookingQueryService([MakeScheduledBooking(SlotStart)]);
        var bookingCommandService = new StubBookingCommandService();
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService);

        var confirmation = await sut.RescheduleBookingAsync("booking-1", newStart);

        Assert.Equal(newStart, confirmation.ScheduledAt);
        var rescheduleCall = Assert.Single(bookingCommandService.RescheduleCalls);
        Assert.Equal("booking-1", rescheduleCall.BookingId);
        Assert.Equal(newStart, rescheduleCall.NewScheduledAt);
    }

    [Fact]
    public async Task RescheduleBookingAsync_BookingCommandServiceThrows_Rethrows()
    {
        var newStart = SlotStart.AddDays(1);
        var bookingQueryService = new StubBookingQueryService([MakeScheduledBooking(SlotStart)]);
        var bookingCommandService = new StubBookingCommandService { ThrowOnReschedule = true };
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("booking-1", newStart));
    }

    [Fact]
    public async Task RescheduleBookingAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var sut = MakeSut(bookingQueryService: new StubBookingQueryService([]));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("no-such-booking", SlotStart));
    }

    [Fact]
    public async Task FullLifecycle_CreateRescheduleThenCancel_AllStepsSucceed()
    {
        // Sprint 3 Commit 7 regression, retained without Calendar: Create -> Reschedule -> Cancel
        // against one shared booking-command stub, verifying the chain still works end to end now
        // that none of the three steps involves Calendar at all.
        var bookingCommandService = new StubBookingCommandService();
        var createSut = MakeSut(bookingCommandService: bookingCommandService);
        var createRequest = new CreateBookingWorkflowRequest(
            "customer-1", "Amelia Hart", "service-1", "Haircut & Style", 60, "$65",
            "specialist-1", "Jordan Lee", SlotStart, string.Empty);

        var confirmation = await createSut.CreateBookingAsync(createRequest);

        var bookingAfterCreate = new AppBookings.BookingDto(
            confirmation.BookingId, "customer-1", "Amelia Hart", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee",
            SlotStart, 60, "$65", AppBookings.BookingStatus.Pending, string.Empty, "org-1", "branch-1");
        var newStart = SlotStart.AddDays(1);
        var rescheduleSut = MakeSut(
            bookingQueryService: new StubBookingQueryService([bookingAfterCreate]),
            bookingCommandService: bookingCommandService);

        await rescheduleSut.RescheduleBookingAsync(confirmation.BookingId, newStart);

        var bookingAfterReschedule = bookingAfterCreate with { ScheduledAt = newStart };
        var cancelSut = MakeSut(
            bookingQueryService: new StubBookingQueryService([bookingAfterReschedule]),
            bookingCommandService: bookingCommandService);

        await cancelSut.CancelBookingAsync(confirmation.BookingId);

        Assert.Single(bookingCommandService.CreateRequests);
        Assert.Single(bookingCommandService.RescheduleCalls);
        Assert.Single(bookingCommandService.UpdateStatusCalls);
        Assert.Equal(AppBookings.BookingStatus.Cancelled, bookingCommandService.UpdateStatusCalls[0].Status);
    }

    // ---- Reception Stabilization Sprint: walk-in / guest customer ----

    [Fact]
    public async Task GetBookingOptionsAsync_CustomerHasLinkedUserAccount_MapsIsLinkedToAccountTrue()
    {
        var customerQueryService = new StubCustomerQueryService([MakeCustomer("customer-1", "Amelia Hart", userId: "user-1")]);
        var sut = MakeSut(customerQueryService);

        var options = await sut.GetBookingOptionsAsync();

        Assert.True(options.Customers[0].IsLinkedToAccount);
    }

    [Fact]
    public async Task GetBookingOptionsAsync_CustomerHasNoLinkedUserAccount_MapsIsLinkedToAccountFalse()
    {
        var customerQueryService = new StubCustomerQueryService([MakeCustomer("customer-2", "Walk-in Guest", userId: null)]);
        var sut = MakeSut(customerQueryService);

        var options = await sut.GetBookingOptionsAsync();

        Assert.False(options.Customers[0].IsLinkedToAccount);
    }

    [Fact]
    public async Task CreateGuestCustomerAsync_CreatesCustomerIdentityAndReturnsUnlinkedOption()
    {
        var customerIdentityService = new StubCustomerIdentityService();
        var sut = MakeSut(customerIdentityService: customerIdentityService);

        var option = await sut.CreateGuestCustomerAsync("Walk-in Guest", "555-0100");

        var createRequest = Assert.Single(customerIdentityService.CreateRequests);
        Assert.Equal("Walk-in Guest", createRequest.FullName);
        Assert.Equal("555-0100", createRequest.PhoneNumber);
        Assert.False(option.IsLinkedToAccount);
        Assert.Equal("Walk-in Guest", option.FullName);
    }
}
