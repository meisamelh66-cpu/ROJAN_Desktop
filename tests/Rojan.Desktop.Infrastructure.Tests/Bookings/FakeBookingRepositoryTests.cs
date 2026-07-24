using Rojan.Desktop.Domain.Bookings;
using Rojan.Desktop.Infrastructure.Bookings;

namespace Rojan.Desktop.Infrastructure.Tests.Bookings;

/// <summary>Smoke + behavioral coverage - same reasoning as Customers.FakeCustomerRepositoryTests.</summary>
public sealed class FakeBookingRepositoryTests
{
    [Fact]
    public async Task GetBookingsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeBookingRepository();

        var result = await sut.GetBookingsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetBookingsAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeBookingRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetBookingsAsync(cts.Token));
    }

    [Fact]
    public async Task GetBookingByIdAsync_KnownId_ReturnsMatchingBooking()
    {
        var sut = new FakeBookingRepository();

        var booking = await sut.GetBookingByIdAsync("booking-1");

        Assert.NotNull(booking);
        Assert.Equal("booking-1", booking.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = new FakeBookingRepository();

        var booking = await sut.GetBookingByIdAsync("no-such-booking");

        Assert.Null(booking);
    }

    [Fact]
    public async Task CreateBookingAsync_NewBooking_BecomesVisibleViaGetBookingsAsync()
    {
        var sut = new FakeBookingRepository();
        var booking = new Booking("booking-new", string.Empty, "Test Customer", string.Empty, "Test Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", BookingStatus.Pending, string.Empty, "org-1", "branch-1");

        await sut.CreateBookingAsync(booking);
        var bookings = await sut.GetBookingsAsync();

        Assert.Contains(bookings, b => b.Id == "booking-new");
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_ExistingBooking_ChangesStatus()
    {
        var sut = new FakeBookingRepository();

        var updated = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Cancelled);
        var reloaded = await sut.GetBookingByIdAsync("booking-1");

        Assert.Equal(BookingStatus.Cancelled, updated.Status);
        Assert.Equal(BookingStatus.Cancelled, reloaded!.Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_UnknownBooking_ThrowsInvalidOperationException()
    {
        var sut = new FakeBookingRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("no-such-booking", BookingStatus.Cancelled));
    }

    [Fact]
    public async Task RescheduleBookingAsync_ExistingBooking_ChangesScheduledAt()
    {
        var sut = new FakeBookingRepository();
        var newScheduledAt = new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);

        var updated = await sut.RescheduleBookingAsync("booking-1", newScheduledAt);
        var reloaded = await sut.GetBookingByIdAsync("booking-1");

        Assert.Equal(newScheduledAt, updated.ScheduledAt);
        Assert.Equal(newScheduledAt, reloaded!.ScheduledAt);
    }

    [Fact]
    public async Task RescheduleBookingAsync_UnknownBooking_ThrowsInvalidOperationException()
    {
        var sut = new FakeBookingRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RescheduleBookingAsync("no-such-booking", DateTimeOffset.UnixEpoch));
    }
}
