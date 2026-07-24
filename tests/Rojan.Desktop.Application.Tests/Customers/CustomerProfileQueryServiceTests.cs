using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Tests.Organizations;
using AppBookings = Rojan.Desktop.Application.Bookings;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Tests.Customers;

public sealed class CustomerProfileQueryServiceTests
{
    private static DomainCustomers.Customer MakeCustomer(string id = "customer-1") =>
        new(id, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            DomainCustomers.CustomerStatus.Vip, "$4,820", new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero), "Notes", "org-1", "branch-1");

    private static AppBookings.BookingDto MakeBooking(
        string id, string customerId, DateTimeOffset scheduledAt, AppBookings.BookingStatus status = AppBookings.BookingStatus.Confirmed) =>
        new(id, customerId, "Amelia Hart", "service-1", "Haircut", "specialist-1", "Alex Stylist",
            scheduledAt, 60, "80", status, string.Empty, "org-1", "branch-1");

    [Fact]
    public async Task GetProfileAsync_CustomerExists_ReturnsCustomerNotesTagsAndActivity()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        repository.Notes.Add(new DomainCustomers.CustomerNote("note-1", "customer-1", "Allergic to certain dyes.", DateTimeOffset.UnixEpoch));
        repository.Tags.Add(new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.UnixEpoch));
        repository.Activities.Add(new DomainCustomers.CustomerActivity("activity-1", "customer-1", "Customer created", DateTimeOffset.UnixEpoch));
        var sut = new CustomerProfileQueryService(repository, new StubBookingQueryService([]), new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal("customer-1", profile.Customer.Id);
        Assert.Equal("Allergic to certain dyes.", Assert.Single(profile.Notes).Text);
        Assert.Equal("VIP", Assert.Single(profile.Tags).Label);
        Assert.Equal("Customer created", Assert.Single(profile.Activity).Description);
    }

    [Fact]
    public async Task GetProfileAsync_CustomerHasNotesAndTags_StatisticsReflectTheirCounts()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        repository.Notes.Add(new DomainCustomers.CustomerNote("note-1", "customer-1", "Note one", DateTimeOffset.UnixEpoch));
        repository.Notes.Add(new DomainCustomers.CustomerNote("note-2", "customer-1", "Note two", DateTimeOffset.UnixEpoch));
        repository.Tags.Add(new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.UnixEpoch));
        var sut = new CustomerProfileQueryService(repository, new StubBookingQueryService([]), new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Contains(profile.Statistics, stat => stat.Label == "یادداشت‌ها" && stat.Value == "2");
        Assert.Contains(profile.Statistics, stat => stat.Label == "برچسب‌ها" && stat.Value == "1");
        Assert.Contains(profile.Statistics, stat => stat.Label == "ارزش کل خرید" && stat.Value == "$4,820");
        Assert.Contains(profile.Statistics, stat => stat.Label == "وضعیت" && stat.Value == "Vip");
    }

    [Fact]
    public async Task GetProfileAsync_CustomerDoesNotExist_ThrowsInvalidOperationException()
    {
        var repository = new StubCustomerRepository([]);
        var sut = new CustomerProfileQueryService(repository, new StubBookingQueryService([]), new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("missing-customer"));
    }

    [Fact]
    public async Task GetProfileAsync_CustomerBelongsToDifferentOrganization_ThrowsAsIfNotFound()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerProfileQueryService(repository, new StubBookingQueryService([]), new StubEnterpriseContext { CurrentOrganizationId = "org-2", CurrentBranchId = "branch-3" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("customer-1"));
    }

    // Sprint 4 Commit 3: customer 360 booking integration.

    [Fact]
    public async Task GetProfileAsync_CustomerHasUpcomingAndPastBookings_SummarySeparatesThemCorrectly()
    {
        var now = DateTimeOffset.Now;
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-past", "customer-1", now.AddDays(-10), AppBookings.BookingStatus.Completed),
            MakeBooking("booking-upcoming", "customer-1", now.AddDays(5)),
        ]);
        var sut = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal("booking-upcoming", Assert.Single(profile.BookingSummary.UpcomingAppointments).Id);
        Assert.Equal("booking-past", Assert.Single(profile.BookingSummary.PastAppointments).Id);
    }

    [Fact]
    public async Task GetProfileAsync_CustomerHasNoBookings_ReturnsEmptyBookingSummary()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerProfileQueryService(repository, new StubBookingQueryService([]), new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Empty(profile.BookingSummary.UpcomingAppointments);
        Assert.Empty(profile.BookingSummary.PastAppointments);
        Assert.Equal(0, profile.BookingSummary.TotalBookingCount);
        Assert.Equal(0, profile.BookingSummary.CompletedBookingCount);
        Assert.Null(profile.BookingSummary.LastAppointmentDate);
    }

    [Fact]
    public async Task GetProfileAsync_OnlyMatchingCustomerIdBookingsAppearInSummary()
    {
        var now = DateTimeOffset.Now;
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-1", "customer-1", now.AddDays(3)),
            MakeBooking("booking-2", "customer-2", now.AddDays(3)),
        ]);
        var sut = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal("booking-1", Assert.Single(profile.BookingSummary.UpcomingAppointments).Id);
        Assert.Equal(1, profile.BookingSummary.TotalBookingCount);
    }

    [Fact]
    public async Task GetProfileAsync_MultipleUpcomingBookings_OrderedSoonestFirst()
    {
        var now = DateTimeOffset.Now;
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-later", "customer-1", now.AddDays(10)),
            MakeBooking("booking-sooner", "customer-1", now.AddDays(2)),
        ]);
        var sut = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal(["booking-sooner", "booking-later"], profile.BookingSummary.UpcomingAppointments.Select(b => b.Id));
    }

    [Fact]
    public async Task GetProfileAsync_MultiplePastBookings_OrderedMostRecentFirstAndLastAppointmentDateIsNewest()
    {
        var now = DateTimeOffset.Now;
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var olderVisit = now.AddDays(-30);
        var newerVisit = now.AddDays(-3);
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-older", "customer-1", olderVisit, AppBookings.BookingStatus.Completed),
            MakeBooking("booking-newer", "customer-1", newerVisit, AppBookings.BookingStatus.Completed),
        ]);
        var sut = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal(["booking-newer", "booking-older"], profile.BookingSummary.PastAppointments.Select(b => b.Id));
        Assert.Equal(newerVisit, profile.BookingSummary.LastAppointmentDate);
    }

    [Fact]
    public async Task GetProfileAsync_BookingCounts_TotalAndCompletedAreCalculatedCorrectly()
    {
        var now = DateTimeOffset.Now;
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-1", "customer-1", now.AddDays(-5), AppBookings.BookingStatus.Completed),
            MakeBooking("booking-2", "customer-1", now.AddDays(-2), AppBookings.BookingStatus.NoShow),
            MakeBooking("booking-3", "customer-1", now.AddDays(4), AppBookings.BookingStatus.Confirmed),
        ]);
        var sut = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal(3, profile.BookingSummary.TotalBookingCount);
        Assert.Equal(1, profile.BookingSummary.CompletedBookingCount);
    }

    // Sprint 4 Commit 5: customer intelligence insights.

    [Fact]
    public async Task GetProfileAsync_CustomerHasNoBookings_ReturnsZeroedInsights()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerProfileQueryService(repository, new StubBookingQueryService([]), new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal(0, profile.Insights.CustomerScore);
        Assert.Equal(LoyaltyLevel.New, profile.Insights.LoyaltyLevel);
        Assert.Equal(EngagementStatus.Inactive, profile.Insights.EngagementStatus);
        Assert.Equal(0, profile.Insights.VisitCount);
        Assert.Equal(0, profile.Insights.CompletedVisitCount);
        Assert.Null(profile.Insights.LastVisitDate);
    }

    [Fact]
    public async Task GetProfileAsync_CustomerHasCompletedBookings_InsightsReflectSameBookingSummaryData()
    {
        var now = DateTimeOffset.Now;
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-1", "customer-1", now.AddDays(-5), AppBookings.BookingStatus.Completed),
            MakeBooking("booking-2", "customer-1", now.AddDays(-15), AppBookings.BookingStatus.Completed),
            MakeBooking("booking-3", "customer-1", now.AddDays(4), AppBookings.BookingStatus.Confirmed),
        ]);
        var sut = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        // Insights is derived from the exact same BookingSummary this method already returns -
        // no independent/duplicated data source.
        Assert.Equal(profile.BookingSummary.TotalBookingCount, profile.Insights.VisitCount);
        Assert.Equal(profile.BookingSummary.CompletedBookingCount, profile.Insights.CompletedVisitCount);
        Assert.Equal(profile.BookingSummary.LastAppointmentDate, profile.Insights.LastVisitDate);
        Assert.Equal(profile.BookingSummary.UpcomingAppointments.Count, profile.Insights.UpcomingVisitCount);
        Assert.Equal(LoyaltyLevel.Regular, profile.Insights.LoyaltyLevel);
        Assert.True(profile.Insights.CustomerScore > 0);
    }

    [Fact]
    public async Task GetProfileAsync_CustomerVisitedLongAgoWithNoUpcomingBooking_EngagementStatusIsAtRiskOrInactive()
    {
        var now = DateTimeOffset.Now;
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-1", "customer-1", now.AddDays(-100), AppBookings.BookingStatus.Completed),
        ]);
        var sut = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal(EngagementStatus.AtRisk, profile.Insights.EngagementStatus);
    }

    // Sprint 4 Commit 6: end-to-end regression tying the sprint's pieces together.

    [Fact]
    public async Task GetProfileAsync_AfterLifecycleTransitionAndCompletedBookings_ReflectsUpdatedStatusAndInsightsTogether()
    {
        // CustomerCommandService's lifecycle transition (Commit 1, via CustomerRules) and
        // CustomerProfileQueryService's booking-driven insights (Commit 3/5) read/write through the
        // same repository but are otherwise fully independent code paths - this verifies they
        // compose correctly end to end, not just in isolation.
        var now = DateTimeOffset.Now;
        var leadCustomer = new DomainCustomers.Customer(
            "customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            DomainCustomers.CustomerStatus.Lead, "$0", now, "Notes", "org-1", "branch-1");
        var repository = new StubCustomerRepository([leadCustomer]);
        var commandService = new CustomerCommandService(repository, new StubEnterpriseContext());
        var bookingQueryService = new StubBookingQueryService([
            MakeBooking("booking-1", "customer-1", now.AddDays(-10), AppBookings.BookingStatus.Completed),
            MakeBooking("booking-2", "customer-1", now.AddDays(-20), AppBookings.BookingStatus.Completed),
            MakeBooking("booking-3", "customer-1", now.AddDays(-30), AppBookings.BookingStatus.Completed),
        ]);
        var profileQueryService = new CustomerProfileQueryService(repository, bookingQueryService, new StubEnterpriseContext());

        // Lead -> Active is a legal transition per CustomerRules.
        await commandService.UpdateCustomerAsync(new UpdateCustomerRequest(
            "customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            CustomerStatus.Active, "$1,200", "Became an active customer"));

        var profile = await profileQueryService.GetProfileAsync("customer-1");

        Assert.Equal(CustomerStatus.Active, profile.Customer.Status);
        Assert.Contains(profile.Statistics, stat => stat.Label == "وضعیت" && stat.Value == "Active");
        Assert.Equal(3, profile.Insights.CompletedVisitCount);
        Assert.Equal(LoyaltyLevel.Loyal, profile.Insights.LoyaltyLevel);
    }
}
