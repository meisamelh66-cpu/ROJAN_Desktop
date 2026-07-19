using Rojan.Desktop.Application.Bookings;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Tests.Bookings;

public sealed class BookingCommandServiceTests
{
    private static DomainBookings.Booking MakeBooking(string id = "booking-1", DomainBookings.BookingStatus status = DomainBookings.BookingStatus.Pending) =>
        new(id, string.Empty, "Amelia Hart", string.Empty, "Colour Touch-Up", string.Empty, "Jordan Lee",
            DateTimeOffset.UnixEpoch, 90, "$120", status, string.Empty);

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
    public async Task CreateBookingAsync_FullyPopulatedRequest_PropagatesIdsAndPrice()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository);
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
        var sut = new BookingCommandService(repository);
        var request = new CreateBookingRequest("Noah Bennett", "Consultation", "Priya Nair", DateTimeOffset.UnixEpoch, 0, string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateBookingAsync(request));
        Assert.Empty(repository.Bookings);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_ValidTransition_UpdatesStatus()
    {
        var repository = new StubBookingRepository([MakeBooking()]);
        var sut = new BookingCommandService(repository);

        var updated = await sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Confirmed);

        Assert.Equal(BookingStatus.Confirmed, updated.Status);
        Assert.Equal(DomainBookings.BookingStatus.Confirmed, Assert.Single(repository.Bookings).Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_IllegalTransition_ThrowsInvalidOperationException()
    {
        var repository = new StubBookingRepository([MakeBooking(status: DomainBookings.BookingStatus.Completed)]);
        var sut = new BookingCommandService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("booking-1", BookingStatus.Pending));
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var repository = new StubBookingRepository();
        var sut = new BookingCommandService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateBookingStatusAsync("no-such-booking", BookingStatus.Cancelled));
    }
}
