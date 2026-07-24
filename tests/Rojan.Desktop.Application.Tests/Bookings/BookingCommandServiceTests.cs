using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.Tests.Organizations;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Tests.Bookings;

public sealed class BookingCommandServiceTests
{
    private static DomainBookings.Booking MakeBooking(string id = "booking-1", DomainBookings.BookingStatus status = DomainBookings.BookingStatus.Pending) =>
        new(id, string.Empty, "Amelia Hart", string.Empty, "Colour Touch-Up", string.Empty, "Jordan Lee",
            DateTimeOffset.UnixEpoch, 90, "$120", status, string.Empty, "org-1", "branch-1");

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_AddsBookingAsPending()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", scheduledAt, 30, string.Empty);

        var created = await sut.CreateBookingAsync(request);

        Assert.Equal("Noah Bennett", created.CustomerName);
        Assert.Equal(BookingStatus.Pending, created.Status);
        Assert.Single(repository.Bookings);
    }

    [Fact]
    public async Task CreateBookingAsync_FullyPopulatedRequest_PropagatesIdsAndPrice()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateBookingRequest(
            "Noah Bennett", "Consultation", "Priya Nair", scheduledAt, 30, string.Empty,
            "customer-4", "service-7", "specialist-2", "$0");

        var created = await sut.CreateBookingAsync(request);

        Assert.Equal("customer-4", created.CustomerId);
        Assert.Equal("service-7", created.ServiceId);
        Assert.Equal("specialist-2", created.SpecialistId);
        Assert.Equal("$0", created.Price);
    }

    [Fact]
    public async Task CreateBookingAsync_InvalidDuration_ThrowsArgumentException()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", DateTimeOffset.UnixEpoch, 0, string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateBookingAsync(request));
        Assert.Empty(repository.Bookings);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_ValidTransition_UpdatesStatus()
    {
        var repository = new StubBookingRepository([MakeBooking()]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var updated = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Confirmed);

        Assert.Equal(BookingStatus.Confirmed, updated.Status);
        Assert.Equal(DomainBookings.BookingStatus.Confirmed, Assert.Single(repository.Bookings).Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_IllegalTransition_ThrowsInvalidOperationException()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Completed)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Pending));
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("no-such-booking", BookingStatus.Cancelled));
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_PendingToInProgress_UpdatesStatus()
    {
        // Sprint 3 Commit 3: a walk-in/early arrival can start service directly from Pending,
        // without first being explicitly Confirmed.
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Pending)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var updated = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.InProgress);

        Assert.Equal(BookingStatus.InProgress, updated.Status);
        Assert.Equal(DomainBookings.BookingStatus.InProgress, Assert.Single(repository.Bookings).Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_ConfirmedToNoShow_UpdatesStatus()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Confirmed)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var updated = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.NoShow);

        Assert.Equal(BookingStatus.NoShow, updated.Status);
        Assert.Equal(DomainBookings.BookingStatus.NoShow, Assert.Single(repository.Bookings).Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_InProgressToCompleted_UpdatesStatus()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.InProgress)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var updated = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Completed);

        Assert.Equal(BookingStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_ConfirmedToCompleted_ThrowsInvalidOperationException()
    {
        // Completed is only reachable via InProgress - Confirmed -> Completed must stay rejected
        // (this is the transition BookingPageViewModel's CompleteBookingCommand used to allow
        // through a stale CanExecute check before Sprint 3 Commit 3 fixed it).
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Confirmed)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Completed));
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_PendingToNoShow_ThrowsInvalidOperationException()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Pending)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("booking-1", BookingStatus.NoShow));
    }

    private static DomainBookings.Booking MakeExistingBooking(
        string id, string specialistId, string specialistName, DateTimeOffset scheduledAt, int durationMinutes,
        DomainBookings.BookingStatus status = DomainBookings.BookingStatus.Pending, string organizationId = "org-1", string branchId = "branch-1") =>
        new(id, string.Empty, "Existing Customer", string.Empty, "Existing Service", specialistId, specialistName,
            scheduledAt, durationMinutes, "$0", status, string.Empty, organizationId, branchId);

    // Sprint 3 Commit 5: quick-add calendar-safety checks. The quick-add form never populates a
    // real SpecialistId (only free-text SpecialistName), so IsSameSpecialist falls back to
    // name-matching whenever either side's id is empty - every test below uses specialistId ""
    // to exercise exactly that path, the one the free-text quick-add form actually takes.

    [Fact]
    public async Task CreateBookingAsync_NoOverlappingBooking_Succeeds()
    {
        // "Available slot creates booking successfully" - an existing booking for the same
        // specialist earlier that day does not conflict with a later, non-overlapping request.
        var existingStart = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", string.Empty, "Priya Nair", existingStart, 30)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var requestStart = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", requestStart, 30, string.Empty);

        var created = await sut.CreateBookingAsync(request);

        Assert.Equal(BookingStatus.Pending, created.Status);
        Assert.Equal(2, repository.Bookings.Count);
    }

    [Fact]
    public async Task CreateBookingAsync_ExactSameSlotAlreadyBooked_ThrowsInvalidOperationException()
    {
        // "Unavailable slot is rejected" - a second request for the exact same specialist/start/duration.
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", string.Empty, "Priya Nair", scheduledAt, 30)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", scheduledAt, 30, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBookingAsync(request));
        Assert.Single(repository.Bookings);
    }

    [Fact]
    public async Task CreateBookingAsync_OverlappingDifferentStartTime_ThrowsInvalidOperationException()
    {
        // "Existing booking conflict is rejected" - the new request starts partway through an
        // existing booking for the same specialist (9:00-10:00 vs. a 9:30 request), not at the
        // exact same instant.
        var existingStart = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", string.Empty, "Priya Nair", existingStart, 60)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var requestStart = existingStart.AddMinutes(30);
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", requestStart, 60, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBookingAsync(request));
        Assert.Single(repository.Bookings);
    }

    [Fact]
    public async Task CreateBookingAsync_SameSpecialistIdConflict_ThrowsInvalidOperationException()
    {
        // Same check, but exercising the id-matching branch of IsSameSpecialist (both sides
        // populated) rather than the name-fallback branch - the Wizard's own creation path.
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", "specialist-2", "Priya Nair", scheduledAt, 30)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var request = new CreateBookingRequest(
            "Noah Bennett", "Consultation", "Priya Nair", scheduledAt, 30, string.Empty,
            "customer-4", "service-7", "specialist-2");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CreateBookingAsync_DifferentSpecialistSameTime_Succeeds()
    {
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", string.Empty, "Priya Nair", scheduledAt, 30)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Jordan Lee", scheduledAt, 30, string.Empty);

        var created = await sut.CreateBookingAsync(request);

        Assert.Equal(BookingStatus.Pending, created.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_ConflictingBookingIsCancelled_Succeeds()
    {
        // A Cancelled booking no longer occupies the specialist's time - only active
        // (Pending/Confirmed/InProgress) bookings count as a conflict.
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository(
            [MakeExistingBooking("booking-1", string.Empty, "Priya Nair", scheduledAt, 30, DomainBookings.BookingStatus.Cancelled)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", scheduledAt, 30, string.Empty);

        var created = await sut.CreateBookingAsync(request);

        Assert.Equal(BookingStatus.Pending, created.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_ConflictingBookingInDifferentBranch_Succeeds()
    {
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository(
            [MakeExistingBooking("booking-1", string.Empty, "Priya Nair", scheduledAt, 30, branchId: "branch-2")]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", scheduledAt, 30, string.Empty);

        var created = await sut.CreateBookingAsync(request);

        Assert.Equal(BookingStatus.Pending, created.Status);
    }
}
