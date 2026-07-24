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

    private static List<DomainBookings.Booking> MakeSearchFixture() =>
    [
        new DomainBookings.Booking(
            "booking-1", "customer-1", "Amelia Hart", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee",
            new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), 60, "$65",
            DomainBookings.BookingStatus.Confirmed, "Prefers quiet chair.", "org-1", "branch-1"),
        new DomainBookings.Booking(
            "booking-2", "customer-2", "Noah Bennett", "service-2", "Colour Touch-Up", "specialist-2", "Priya Nair",
            new DateTimeOffset(2026, 3, 5, 14, 0, 0, TimeSpan.Zero), 90, "$120",
            DomainBookings.BookingStatus.Pending, string.Empty, "org-1", "branch-1"),
        new DomainBookings.Booking(
            "booking-3", "customer-3", "Olivia Chen", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee",
            new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero), 60, "$65",
            DomainBookings.BookingStatus.Cancelled, "Rescheduled twice already.", "org-1", "branch-1"),
    ];

    [Fact]
    public async Task SearchBookingsAsync_EmptyFilter_ReturnsAllBookingsSameAsGetBookingsAsync()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var searchResult = await sut.SearchBookingsAsync(new BookingSearchFilter());
        var getResult = await sut.GetBookingsAsync();

        Assert.Equal(3, searchResult.Count);
        Assert.Equal(getResult.Select(booking => booking.Id), searchResult.Select(booking => booking.Id));
    }

    [Fact]
    public async Task SearchBookingsAsync_CustomerNameFilter_ReturnsOnlyMatchingCustomer()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchBookingsAsync(new BookingSearchFilter(CustomerName: "amelia"));

        Assert.Equal("booking-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchBookingsAsync_ServiceNameFilter_ReturnsOnlyMatchingService()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchBookingsAsync(new BookingSearchFilter(ServiceName: "colour"));

        Assert.Equal("booking-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchBookingsAsync_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchBookingsAsync(new BookingSearchFilter(Status: BookingStatus.Cancelled));

        Assert.Equal("booking-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchBookingsAsync_DateRangeFilter_ReturnsOnlyBookingsWithinRange()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchBookingsAsync(new BookingSearchFilter(
            DateFrom: new DateOnly(2026, 3, 3),
            DateTo: new DateOnly(2026, 3, 8)));

        Assert.Equal("booking-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchBookingsAsync_SearchText_MatchesAcrossCustomerServiceSpecialistAndNotes()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var matchesCustomer = await sut.SearchBookingsAsync(new BookingSearchFilter(SearchText: "bennett"));
        var matchesService = await sut.SearchBookingsAsync(new BookingSearchFilter(SearchText: "touch-up"));
        var matchesSpecialist = await sut.SearchBookingsAsync(new BookingSearchFilter(SearchText: "priya"));
        var matchesNotes = await sut.SearchBookingsAsync(new BookingSearchFilter(SearchText: "quiet chair"));

        Assert.Equal("booking-2", Assert.Single(matchesCustomer).Id);
        Assert.Equal("booking-2", Assert.Single(matchesService).Id);
        Assert.Equal("booking-2", Assert.Single(matchesSpecialist).Id);
        Assert.Equal("booking-1", Assert.Single(matchesNotes).Id);
    }

    [Fact]
    public async Task SearchBookingsAsync_CombinedFilters_AreAnded()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchBookingsAsync(new BookingSearchFilter(
            ServiceName: "haircut", Status: BookingStatus.Cancelled));

        Assert.Equal("booking-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchBookingsAsync_NoMatch_ReturnsEmptyList()
    {
        var repository = new StubBookingRepository(MakeSearchFixture());
        var sut = new BookingQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchBookingsAsync(new BookingSearchFilter(CustomerName: "no-such-customer"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchBookingsAsync_BookingInDifferentOrganization_IsExcluded()
    {
        var domainBooking = new DomainBookings.Booking(
            "booking-99", string.Empty, "Other Org Customer", string.Empty, "Service", string.Empty, string.Empty,
            DateTimeOffset.UnixEpoch, 60, "$0", DomainBookings.BookingStatus.Pending, string.Empty, "org-2", "branch-3");
        var repository = new StubBookingRepository([domainBooking]);
        var sut = new BookingQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });

        var result = await sut.SearchBookingsAsync(new BookingSearchFilter());

        Assert.Empty(result);
    }
}
