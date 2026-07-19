using Rojan.Desktop.Application.Bookings;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Tests.Bookings;

public sealed class BookingCommandServiceTests
{
    private static DomainBookings.Booking MakeBooking(string id = "booking-1") =>
        new(id, string.Empty, "Amelia Hart", "Colour Touch-Up", "Jordan Lee",
            DateTimeOffset.UnixEpoch, 90, DomainBookings.BookingStatus.Pending, string.Empty);

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_AddsBookingAsPending()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository);
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", scheduledAt, 30, string.Empty);

        var created = await sut.CreateBookingAsync(request);

        Assert.Equal("Noah Bennett", created.CustomerName);
        Assert.Equal(BookingStatus.Pending, created.Status);
        Assert.Single(repository.Bookings);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_ValidId_UpdatesStatus()
    {
        var repository = new StubBookingRepository([MakeBooking()]);
        var sut = new BookingCommandService(repository);

        var updated = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Confirmed);

        Assert.Equal(BookingStatus.Confirmed, updated.Status);
        Assert.Equal(DomainBookings.BookingStatus.Confirmed, Assert.Single(repository.Bookings).Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("no-such-booking", BookingStatus.Cancelled));
    }
}
