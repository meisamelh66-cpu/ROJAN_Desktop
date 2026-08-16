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
        StubCalendarCommandService? calendarCommandService = null,
        StubBookingQueryService? bookingQueryService = null,
        StubBookingCommandService? bookingCommandService = null,
        StubCustomerCommandService? customerCommandService = null) => new(
        customerQueryService ?? new StubCustomerQueryService([]),
        serviceQueryService ?? new StubServiceQueryService([]),
        specialistQueryService ?? new StubSpecialistQueryService([]),
        calendarQueryService ?? new StubCalendarQueryService(),
        calendarCommandService ?? new StubCalendarCommandService(),
        bookingQueryService ?? new StubBookingQueryService(),
        bookingCommandService ?? new StubBookingCommandService(),
        customerCommandService ?? new StubCustomerCommandService());

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
            SlotStart, 60, "$65", AppBookings.BookingStatus.Confirmed, string.Empty, "org-1", "branch-1");
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
            SlotStart, 240, "$0", AppBookings.BookingStatus.Pending, string.Empty, "org-1", "branch-1");
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

    // Sprint 3 Commit 6: reschedule workflow. Mirrors CreateBookingAsync's own
    // reserve-before-write, release-on-failure rollback shape.

    private static AppBookings.BookingDto MakeScheduledBooking(DateTimeOffset scheduledAt, string specialistId = "specialist-1") =>
        new("booking-1", "customer-1", "Amelia Hart", "service-1", "Haircut & Style", specialistId, "Jordan Lee",
            scheduledAt, 60, "$65", AppBookings.BookingStatus.Confirmed, string.Empty, "org-1", "branch-1");

    [Fact]
    public async Task RescheduleBookingAsync_BookingHasSpecialist_ReservesNewSlotThenReleasesOldSlot()
    {
        var newStart = SlotStart.AddDays(1);
        var bookingQueryService = new StubBookingQueryService([MakeScheduledBooking(SlotStart)]);
        var bookingCommandService = new StubBookingCommandService();
        var calendarCommandService = new StubCalendarCommandService();
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService, calendarCommandService: calendarCommandService);

        var confirmation = await sut.RescheduleBookingAsync("booking-1", newStart);

        Assert.Equal(newStart, confirmation.ScheduledAt);
        var reserveCall = Assert.Single(calendarCommandService.ReserveCalls);
        Assert.Equal("specialist-1", reserveCall.SpecialistId);
        Assert.Equal(newStart, reserveCall.Start);
        Assert.Equal(newStart.AddMinutes(60), reserveCall.End);
        var releaseCall = Assert.Single(calendarCommandService.ReleaseCalls);
        Assert.Equal("specialist-1", releaseCall.SpecialistId);
        Assert.Equal(SlotStart, releaseCall.Start);
        var rescheduleCall = Assert.Single(bookingCommandService.RescheduleCalls);
        Assert.Equal("booking-1", rescheduleCall.BookingId);
        Assert.Equal(newStart, rescheduleCall.NewScheduledAt);
    }

    [Fact]
    public async Task RescheduleBookingAsync_NewSlotUnavailable_ThrowsAndNeverTouchesBookingOrOldSlot()
    {
        // "Reschedule rejects unavailable target slot" + "do not lose the original reservation".
        var newStart = SlotStart.AddDays(1);
        var bookingQueryService = new StubBookingQueryService([MakeScheduledBooking(SlotStart)]);
        var bookingCommandService = new StubBookingCommandService();
        var calendarCommandService = new StubCalendarCommandService { ThrowOnReserve = true };
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService, calendarCommandService: calendarCommandService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("booking-1", newStart));

        Assert.Empty(bookingCommandService.RescheduleCalls);
        Assert.Empty(calendarCommandService.ReleaseCalls);
    }

    [Fact]
    public async Task RescheduleBookingAsync_BookingUpdateFails_ReleasesNewlyReservedSlotAndRethrows()
    {
        // "Failed reschedule keeps original booking intact" from the Calendar side: the new slot
        // was reserved, then the booking-level move failed (e.g. a same-specialist conflict), so
        // the new reservation must be released - and since the booking never actually moved, the
        // *old* slot must never be released either.
        var newStart = SlotStart.AddDays(1);
        var bookingQueryService = new StubBookingQueryService([MakeScheduledBooking(SlotStart)]);
        var bookingCommandService = new StubBookingCommandService { ThrowOnReschedule = true };
        var calendarCommandService = new StubCalendarCommandService();
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService, calendarCommandService: calendarCommandService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("booking-1", newStart));

        Assert.Single(calendarCommandService.ReserveCalls);
        var releaseCall = Assert.Single(calendarCommandService.ReleaseCalls);
        Assert.Equal(newStart, releaseCall.Start);
    }

    [Fact]
    public async Task RescheduleBookingAsync_BookingHasNoSpecialist_ReschedulesWithoutAnyCalendarCalls()
    {
        var newStart = SlotStart.AddDays(1);
        var booking = new AppBookings.BookingDto(
            "booking-3", string.Empty, "Olivia Chen", string.Empty, "Corporate Group Styling", string.Empty, "Priya Nair",
            SlotStart, 240, "$0", AppBookings.BookingStatus.Pending, string.Empty, "org-1", "branch-1");
        var bookingQueryService = new StubBookingQueryService([booking]);
        var bookingCommandService = new StubBookingCommandService();
        var calendarCommandService = new StubCalendarCommandService();
        var sut = MakeSut(bookingQueryService: bookingQueryService, bookingCommandService: bookingCommandService, calendarCommandService: calendarCommandService);

        await sut.RescheduleBookingAsync("booking-3", newStart);

        Assert.Empty(calendarCommandService.ReserveCalls);
        Assert.Empty(calendarCommandService.ReleaseCalls);
        Assert.Single(bookingCommandService.RescheduleCalls);
    }

    [Fact]
    public async Task RescheduleBookingAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var sut = MakeSut(bookingQueryService: new StubBookingQueryService([]));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("no-such-booking", SlotStart));
    }

    [Fact]
    public async Task FullLifecycle_CreateRescheduleThenCancel_CalendarStaysInSyncThroughout()
    {
        // Sprint 3 Commit 7 regression: Create -> Reschedule -> Cancel against one shared
        // Calendar/booking command stub pair, verifying the reserve/release calls stay
        // consistent across the whole lifecycle - not just each operation in isolation. In
        // particular, Cancel must release the slot at the booking's RESCHEDULED time, not its
        // original one.
        var calendarCommandService = new StubCalendarCommandService();
        var bookingCommandService = new StubBookingCommandService();
        var createSut = MakeSut(calendarCommandService: calendarCommandService, bookingCommandService: bookingCommandService);
        var createRequest = new CreateBookingWorkflowRequest(
            "customer-1", "Amelia Hart", "service-1", "Haircut & Style", 60, "$65",
            "specialist-1", "Jordan Lee", SlotStart, string.Empty);

        var confirmation = await createSut.CreateBookingAsync(createRequest);
        Assert.Single(calendarCommandService.ReserveCalls);

        // Reschedule needs to read the booking back at its post-create state.
        var bookingAfterCreate = new AppBookings.BookingDto(
            confirmation.BookingId, "customer-1", "Amelia Hart", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee",
            SlotStart, 60, "$65", AppBookings.BookingStatus.Pending, string.Empty, "org-1", "branch-1");
        var newStart = SlotStart.AddDays(1);
        var rescheduleSut = MakeSut(
            bookingQueryService: new StubBookingQueryService([bookingAfterCreate]),
            bookingCommandService: bookingCommandService,
            calendarCommandService: calendarCommandService);

        await rescheduleSut.RescheduleBookingAsync(confirmation.BookingId, newStart);

        Assert.Equal(2, calendarCommandService.ReserveCalls.Count); // create's slot + reschedule's new slot
        var releaseAfterReschedule = Assert.Single(calendarCommandService.ReleaseCalls);
        Assert.Equal(SlotStart, releaseAfterReschedule.Start); // reschedule released the ORIGINAL slot

        // Cancel needs to read the booking back at its post-reschedule state.
        var bookingAfterReschedule = bookingAfterCreate with { ScheduledAt = newStart };
        var cancelSut = MakeSut(
            bookingQueryService: new StubBookingQueryService([bookingAfterReschedule]),
            bookingCommandService: bookingCommandService,
            calendarCommandService: calendarCommandService);

        await cancelSut.CancelBookingAsync(confirmation.BookingId);

        Assert.Equal(2, calendarCommandService.ReleaseCalls.Count);
        var releaseAfterCancel = calendarCommandService.ReleaseCalls[^1];
        Assert.Equal(newStart, releaseAfterCancel.Start); // cancel released the RESCHEDULED slot, not the original
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
    public async Task CreateGuestCustomerAsync_CreatesCustomerAndReturnsUnlinkedOption()
    {
        var customerCommandService = new StubCustomerCommandService();
        var sut = MakeSut(customerCommandService: customerCommandService);

        var option = await sut.CreateGuestCustomerAsync("Walk-in Guest", "555-0100");

        var createRequest = Assert.Single(customerCommandService.CreateRequests);
        Assert.Equal("Walk-in Guest", createRequest.FullName);
        Assert.Equal("555-0100", createRequest.Phone);
        Assert.False(option.IsLinkedToAccount);
        Assert.Equal("Walk-in Guest", option.FullName);
    }
}
