using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.Tests.Organizations;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Tests.Bookings;

public sealed class BookingQueryServiceTests
{
    [Fact]
    public async Task GetBookingsAsync_RepositoryReturnsBookings_MapsEveryFieldToDto()
    {
        var scheduledAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var domainBooking = new DomainBookings.Booking(
            "booking-1", "customer-1", "Amelia Hart", "service-2", "Colour Touch-Up", "specialist-1", "Jordan Lee",
            scheduledAt, 90, "$120", DomainBookings.BookingStatus.Confirmed, "Notes", "org-1", "branch-1");
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetBookingsAsync();

        var dto = Assert.Single(result);
        Assert.Equal(domainBooking.Id, dto.Id);
        Assert.Equal(domainBooking.CustomerId, dto.CustomerId);
        Assert.Equal(domainBooking.CustomerName, dto.CustomerName);
        Assert.Equal(domainBooking.ServiceId, dto.ServiceId);
        Assert.Equal(domainBooking.ServiceName, dto.ServiceName);
        Assert.Equal(domainBooking.SpecialistId, dto.SpecialistId);
        Assert.Equal(domainBooking.SpecialistName, dto.SpecialistName);
        Assert.Equal(domainBooking.ScheduledAt, dto.ScheduledAt);
        Assert.Equal(domainBooking.DurationMinutes, dto.DurationMinutes);
        Assert.Equal(domainBooking.Price, dto.Price);
        Assert.Equal(BookingStatus.Confirmed, dto.Status);
        Assert.Equal(domainBooking.Notes, dto.Notes);
    }

    [Fact]
    public async Task GetBookingsAsync_RepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        var repository = new StubBookingRepository([]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetBookingsAsync();

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(DomainBookings.BookingStatus.Pending, BookingStatus.Pending)]
    [InlineData(DomainBookings.BookingStatus.Confirmed, BookingStatus.Confirmed)]
    [InlineData(DomainBookings.BookingStatus.InProgress, BookingStatus.InProgress)]
    [InlineData(DomainBookings.BookingStatus.Completed, BookingStatus.Completed)]
    [InlineData(DomainBookings.BookingStatus.Cancelled, BookingStatus.Cancelled)]
    [InlineData(DomainBookings.BookingStatus.NoShow, BookingStatus.NoShow)]
    public async Task GetBookingsAsync_EachDomainStatus_MapsToMatchingApplicationStatus(
        DomainBookings.BookingStatus domainStatus, BookingStatus expectedStatus)
    {
        var domainBooking = new DomainBookings.Booking(
            "booking-1", string.Empty, "Test Customer", string.Empty, "Test Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", domainStatus, string.Empty, "org-1", "branch-1");
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetBookingsAsync();

        Assert.Equal(expectedStatus, Assert.Single(result).Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_KnownId_ReturnsMappedDto()
    {
        var domainBooking = new DomainBookings.Booking(
            "booking-1", string.Empty, "Amelia Hart", string.Empty, "Colour Touch-Up", string.Empty, "Jordan Lee",
            DateTimeOffset.UnixEpoch, 90, "$120", DomainBookings.BookingStatus.Confirmed, string.Empty, "org-1", "branch-1");
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetBookingByIdAsync("booking-1");

        Assert.NotNull(result);
        Assert.Equal("booking-1", result.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_UnknownId_ReturnsNull()
    {
        var repository = new StubBookingRepository([]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetBookingByIdAsync("no-such-booking");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingsAsync_BookingInDifferentOrganization_IsExcluded()
    {
        var domainBooking = new DomainBookings.Booking(
            "booking-99", string.Empty, "Other Org Customer", string.Empty, "Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", DomainBookings.BookingStatus.Pending, string.Empty, "org-2", "branch-3");
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });

        var result = await sut.GetBookingsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingInDifferentBranch_ReturnsNull()
    {
        var domainBooking = new DomainBookings.Booking(
            "booking-98", string.Empty, "Other Branch Customer", string.Empty, "Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", DomainBookings.BookingStatus.Pending, string.Empty, "org-1", "branch-2");
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });

        var result = await sut.GetBookingByIdAsync("booking-98");

        Assert.Null(result);
    }
}
