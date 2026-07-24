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

    // Sprint 3 Commit 6: reschedule support. RescheduleBookingAsync reuses the exact same
    // conflict-detection (now EnsureNoConflictAsync) Commit 5 introduced, excluding the booking's
    // own current record from the scan, plus a new eligibility check (only active bookings can be
    // rescheduled).

    [Fact]
    public async Task RescheduleBookingAsync_NoConflict_UpdatesScheduledAt()
    {
        var originalStart = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var newStart = new DateTimeOffset(2026, 3, 2, 14, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", "specialist-1", "Jordan Lee", originalStart, 60)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var updated = await sut.RescheduleBookingAsync("booking-1", newStart);

        Assert.Equal(newStart, updated.ScheduledAt);
        Assert.Equal(newStart, Assert.Single(repository.Bookings).ScheduledAt);
    }

    [Fact]
    public async Task RescheduleBookingAsync_DoesNotConflictWithItself()
    {
        // The booking being rescheduled must be excluded from its own conflict scan - without
        // that exclusion, every reschedule attempt would "conflict" with its own current record.
        var originalStart = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", "specialist-1", "Jordan Lee", originalStart, 60)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        // Reschedule to a slightly later time that still overlaps the booking's *own* original slot.
        var updated = await sut.RescheduleBookingAsync("booking-1", originalStart.AddMinutes(15));

        Assert.Equal(originalStart.AddMinutes(15), updated.ScheduledAt);
    }

    [Fact]
    public async Task RescheduleBookingAsync_ConflictsWithAnotherActiveBooking_ThrowsInvalidOperationException()
    {
        var otherStart = new DateTimeOffset(2026, 3, 5, 9, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository(
        [
            MakeExistingBooking("booking-1", "specialist-1", "Jordan Lee", new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), 60),
            MakeExistingBooking("booking-2", "specialist-1", "Jordan Lee", otherStart, 60),
        ]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("booking-1", otherStart));

        // "Failed reschedule keeps original booking intact" - booking-1 must still be at its
        // original time after the rejected reschedule.
        var stillOriginal = repository.Bookings.Single(booking => booking.Id == "booking-1");
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), stillOriginal.ScheduledAt);
    }

    [Theory]
    [InlineData(DomainBookings.BookingStatus.Completed)]
    [InlineData(DomainBookings.BookingStatus.Cancelled)]
    [InlineData(DomainBookings.BookingStatus.NoShow)]
    public async Task RescheduleBookingAsync_TerminalStatus_ThrowsInvalidOperationException(DomainBookings.BookingStatus status)
    {
        var originalStart = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var repository = new StubBookingRepository([MakeExistingBooking("booking-1", "specialist-1", "Jordan Lee", originalStart, 60, status)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("booking-1", originalStart.AddDays(1)));
        Assert.Equal(originalStart, repository.Bookings.Single().ScheduledAt);
    }

    [Fact]
    public async Task RescheduleBookingAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("no-such-booking", DateTimeOffset.UnixEpoch));
    }

    // Sprint 3 Commit 7 regression: full lifecycle walks on one booking, not just each transition
    // tested in isolation - proves the whole chain of Sprint 3's changes (InProgress/NoShow states,
    // the CompleteBookingCommand CanExecute fix, reschedule eligibility) hold together end-to-end.

    [Fact]
    public async Task BookingLifecycle_PendingThroughCompleted_AllTransitionsSucceedInOrder()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Pending)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var confirmed = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Confirmed);
        Assert.Equal(BookingStatus.Confirmed, confirmed.Status);

        var inProgress = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.InProgress);
        Assert.Equal(BookingStatus.InProgress, inProgress.Status);

        var completed = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Completed);
        Assert.Equal(BookingStatus.Completed, completed.Status);

        Assert.Equal(DomainBookings.BookingStatus.Completed, Assert.Single(repository.Bookings).Status);
    }

    [Fact]
    public async Task BookingLifecycle_PendingToCancelled_Succeeds()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Pending)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var cancelled = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Cancelled);

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task BookingLifecycle_ConfirmedToNoShow_Succeeds()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Confirmed)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        var noShow = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.NoShow);

        Assert.Equal(BookingStatus.NoShow, noShow.Status);
    }

    [Fact]
    public async Task BookingLifecycle_ReachedTerminalStatus_RejectsBothFurtherTransitionAndReschedule()
    {
        // Once a booking reaches a terminal status, both write paths (status transition and
        // reschedule) must reject it consistently.
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Completed)]);
        var sut = new BookingCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Confirmed));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("booking-1", DateTimeOffset.UnixEpoch.AddDays(1)));
    }
}
