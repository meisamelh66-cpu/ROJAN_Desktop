using Rojan.Desktop.Application.Bookings;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Tests.Bookings;

public sealed class BookingQueryServiceTests
{
    [Fact]
    public async Task GetBookingsAsync_RepositoryReturnsBookings_MapsEveryFieldToDto()
    {
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var domainBooking = new DomainBookings.Booking(
            "booking-1", string.Empty, "Amelia Hart", "Colour Touch-Up", "Jordan Lee",
            scheduledAt, 90, DomainBookings.BookingStatus.Confirmed, "Notes");
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository);

        var result = await sut.GetBookingsAsync();

        var dto = Assert.Single(result);
        Assert.Equal(domainBooking.Id, dto.Id);
        Assert.Equal(domainBooking.CustomerName, dto.CustomerName);
        Assert.Equal(domainBooking.ServiceName, dto.ServiceName);
        Assert.Equal(domainBooking.SpecialistName, dto.SpecialistName);
        Assert.Equal(domainBooking.ScheduledAt, dto.ScheduledAt);
        Assert.Equal(domainBooking.DurationMinutes, dto.DurationMinutes);
        Assert.Equal(BookingStatus.Confirmed, dto.Status);
        Assert.Equal(domainBooking.Notes, dto.Notes);
    }

    [Fact]
    public async Task GetBookingsAsync_RepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        var repository = new StubBookingRepository([]);
        var sut = new BookingQueryService(repository);

        var result = await sut.GetBookingsAsync();

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(DomainBookings.BookingStatus.Pending, BookingStatus.Pending)]
    [InlineData(DomainBookings.BookingStatus.Confirmed, BookingStatus.Confirmed)]
    [InlineData(DomainBookings.BookingStatus.Completed, BookingStatus.Completed)]
    [InlineData(DomainBookings.BookingStatus.Cancelled, BookingStatus.Cancelled)]
    public async Task GetBookingsAsync_EachDomainStatus_MapsToMatchingApplicationStatus(
        DomainBookings.BookingStatus domainStatus, BookingStatus expectedStatus)
    {
        var domainBooking = new DomainBookings.Booking(
            "booking-1", string.Empty, "Test Customer", "Test Service", string.Empty,
            DateTimeOffset.UnixEpoch, 60, domainStatus, string.Empty);
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository);

        var result = await sut.GetBookingsAsync();

        Assert.Equal(expectedStatus, Assert.Single(result).Status);
    }
}
