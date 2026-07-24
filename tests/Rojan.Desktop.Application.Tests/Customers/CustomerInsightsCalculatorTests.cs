using Rojan.Desktop.Application.Customers;
using AppBookings = Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Application.Tests.Customers;

public sealed class CustomerInsightsCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static AppBookings.BookingDto MakeBooking(
        string id, DateTimeOffset scheduledAt, AppBookings.BookingStatus status = AppBookings.BookingStatus.Confirmed) =>
        new(id, "customer-1", "Amelia Hart", "service-1", "Haircut", "specialist-1", "Alex Stylist",
            scheduledAt, 60, "80", status, string.Empty, "org-1", "branch-1");

    private static CustomerActivityDto MakeActivity(DateTimeOffset occurredAt) =>
        new("activity-1", "customer-1", "Note added", occurredAt);

    private static CustomerBookingSummaryDto MakeSummary(
        int completedBookingCount,
        int upcomingCount = 0,
        DateTimeOffset? lastAppointmentDate = null,
        int? totalBookingCountOverride = null)
    {
        var upcoming = Enumerable.Range(0, upcomingCount)
            .Select(index => MakeBooking($"upcoming-{index}", Now.AddDays(index + 1)))
            .ToList();
        var totalBookingCount = totalBookingCountOverride ?? (completedBookingCount + upcomingCount);

        return new CustomerBookingSummaryDto(upcoming, [], totalBookingCount, completedBookingCount, lastAppointmentDate);
    }

    [Fact]
    public void Calculate_CustomerWithNoBookings_ReturnsZeroedInsights()
    {
        var insights = CustomerInsightsCalculator.Calculate(CustomerBookingSummaryDto.Empty, [], Now);

        Assert.Equal(0, insights.CustomerScore);
        Assert.Equal(LoyaltyLevel.New, insights.LoyaltyLevel);
        Assert.Equal(EngagementStatus.Inactive, insights.EngagementStatus);
        Assert.Equal(0, insights.VisitCount);
        Assert.Equal(0, insights.CompletedVisitCount);
        Assert.Null(insights.LastVisitDate);
        Assert.Equal(0, insights.UpcomingVisitCount);
        Assert.Null(insights.DaysSinceLastVisit);
    }

    [Theory]
    [InlineData(0, LoyaltyLevel.New)]
    [InlineData(1, LoyaltyLevel.Regular)]
    [InlineData(2, LoyaltyLevel.Regular)]
    [InlineData(3, LoyaltyLevel.Loyal)]
    [InlineData(5, LoyaltyLevel.Loyal)]
    [InlineData(6, LoyaltyLevel.Vip)]
    [InlineData(10, LoyaltyLevel.Vip)]
    public void Calculate_CompletedBookingCount_DeterminesLoyaltyLevel(int completedBookingCount, LoyaltyLevel expected)
    {
        var summary = MakeSummary(completedBookingCount, lastAppointmentDate: Now.AddDays(-10));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(expected, insights.LoyaltyLevel);
    }

    [Fact]
    public void Calculate_CustomerWithCompletedBookings_VisitCountsPassThroughFromBookingSummary()
    {
        var summary = MakeSummary(completedBookingCount: 4, upcomingCount: 2, lastAppointmentDate: Now.AddDays(-5));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(summary.TotalBookingCount, insights.VisitCount);
        Assert.Equal(4, insights.CompletedVisitCount);
        Assert.Equal(2, insights.UpcomingVisitCount);
        Assert.Equal(summary.LastAppointmentDate, insights.LastVisitDate);
        Assert.Equal(5, insights.DaysSinceLastVisit);
    }

    [Fact]
    public void Calculate_HasUpcomingBooking_EngagementStatusIsActiveRegardlessOfLastVisitRecency()
    {
        // A customer who already has a booking on the calendar has demonstrated engagement,
        // even if their most recent completed visit was a long time ago.
        var summary = MakeSummary(completedBookingCount: 1, upcomingCount: 1, lastAppointmentDate: Now.AddDays(-200));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(EngagementStatus.Active, insights.EngagementStatus);
    }

    [Fact]
    public void Calculate_NoUpcomingBookingAndRecentVisit_EngagementStatusIsActive()
    {
        var summary = MakeSummary(completedBookingCount: 2, lastAppointmentDate: Now.AddDays(-30));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(EngagementStatus.Active, insights.EngagementStatus);
    }

    [Fact]
    public void Calculate_NoUpcomingBookingAndModeratelyStaleVisit_EngagementStatusIsAtRisk()
    {
        var summary = MakeSummary(completedBookingCount: 2, lastAppointmentDate: Now.AddDays(-90));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(EngagementStatus.AtRisk, insights.EngagementStatus);
    }

    [Fact]
    public void Calculate_NoUpcomingBookingAndVeryStaleVisit_EngagementStatusIsInactive()
    {
        var summary = MakeSummary(completedBookingCount: 2, lastAppointmentDate: Now.AddDays(-200));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(EngagementStatus.Inactive, insights.EngagementStatus);
    }

    [Fact]
    public void Calculate_NeverVisited_EngagementStatusIsInactive()
    {
        var summary = MakeSummary(completedBookingCount: 0);

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(EngagementStatus.Inactive, insights.EngagementStatus);
    }

    [Fact]
    public void Calculate_CompletedAndUpcomingBookingsWithRecentVisit_ScoreCombinesAllFactors()
    {
        // 2 completed (30) + 1 upcoming (5) + recent-visit bonus (<=30 days: 20) = 55.
        var summary = MakeSummary(completedBookingCount: 2, upcomingCount: 1, lastAppointmentDate: Now.AddDays(-10));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(55, insights.CustomerScore);
    }

    [Fact]
    public void Calculate_RecentActivityWithNoBookings_AddsScoreBonus()
    {
        // No visits at all, but a note/tag/activity entry was recorded within the last 30 days -
        // "recent activity" is its own scoring component, independent of visit history.
        var summary = MakeSummary(completedBookingCount: 0);
        var activity = new List<CustomerActivityDto> { MakeActivity(Now.AddDays(-5)) };

        var insights = CustomerInsightsCalculator.Calculate(summary, activity, Now);

        Assert.Equal(10, insights.CustomerScore);
    }

    [Fact]
    public void Calculate_OldActivityBeyondThirtyDays_DoesNotAddScoreBonus()
    {
        var summary = MakeSummary(completedBookingCount: 0);
        var activity = new List<CustomerActivityDto> { MakeActivity(Now.AddDays(-60)) };

        var insights = CustomerInsightsCalculator.Calculate(summary, activity, Now);

        Assert.Equal(0, insights.CustomerScore);
    }

    [Fact]
    public void Calculate_ManyCompletedAndUpcomingBookings_ScoreIsClampedToOneHundred()
    {
        var summary = MakeSummary(completedBookingCount: 10, upcomingCount: 5, lastAppointmentDate: Now.AddDays(-1));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(100, insights.CustomerScore);
    }

    [Fact]
    public void Calculate_ModeratelyStaleVisitInsteadOfRecent_ScoreGetsSmallerRecencyBonus()
    {
        // 1 completed (15) + stale-but-not-ancient visit bonus (31-90 days: 10) = 25.
        var summary = MakeSummary(completedBookingCount: 1, lastAppointmentDate: Now.AddDays(-45));

        var insights = CustomerInsightsCalculator.Calculate(summary, [], Now);

        Assert.Equal(25, insights.CustomerScore);
    }
}
