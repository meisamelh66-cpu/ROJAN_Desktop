namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Customer Intelligence (Sprint 4 Commit 5): pure, stateless calculation of
/// <see cref="CustomerInsightsDto"/> from a customer's existing
/// <see cref="CustomerBookingSummaryDto"/> and <see cref="CustomerActivityDto"/>
/// history - no repository access, no I/O, so every rule here is unit
/// testable without any test-double plumbing (the same "pure predicate"
/// shape <c>Domain.Customers.CustomerRules</c> uses, deliberately kept in
/// Application instead since this is a calculated *read-model* value, never
/// a Domain lifecycle rule the customer record itself enforces).
/// <see cref="CustomerProfileQueryService"/> is the only caller; the
/// current time is threaded through <see cref="Calculate"/> explicitly
/// rather than read from <see cref="DateTimeOffset.Now"/> internally, so
/// every day-based rule below stays deterministic and testable.
/// </summary>
public static class CustomerInsightsCalculator
{
    public static CustomerInsightsDto Calculate(
        CustomerBookingSummaryDto bookingSummary,
        IReadOnlyList<CustomerActivityDto> activity,
        DateTimeOffset now)
    {
        var daysSinceLastVisit = bookingSummary.LastAppointmentDate.HasValue
            ? (int?)Math.Max(0, (now - bookingSummary.LastAppointmentDate.Value).Days)
            : null;

        var lastActivityAt = activity.Count == 0 ? (DateTimeOffset?)null : activity.Max(entry => entry.OccurredAt);
        var daysSinceLastActivity = lastActivityAt.HasValue
            ? (int?)Math.Max(0, (now - lastActivityAt.Value).Days)
            : null;

        var upcomingVisitCount = bookingSummary.UpcomingAppointments.Count;

        return new CustomerInsightsDto(
            CalculateScore(bookingSummary.CompletedBookingCount, upcomingVisitCount, daysSinceLastVisit, daysSinceLastActivity),
            CalculateLoyaltyLevel(bookingSummary.CompletedBookingCount),
            CalculateEngagementStatus(upcomingVisitCount, daysSinceLastVisit),
            bookingSummary.TotalBookingCount,
            bookingSummary.CompletedBookingCount,
            bookingSummary.LastAppointmentDate,
            upcomingVisitCount,
            daysSinceLastVisit);
    }

    /// <summary>
    /// Visit frequency (completed bookings, 15 points each) plus upcoming
    /// bookings (5 points each, a forward-looking loyalty signal) plus a
    /// recency bonus for a visit or any recorded activity within the last
    /// 30/90 days - the "recent activity" component the calling task
    /// description calls for. Clamped to [0, 100] so the score always
    /// reads as a percentage-like figure regardless of how many bookings a
    /// long-standing customer accumulates.
    /// </summary>
    private static int CalculateScore(int completedBookingCount, int upcomingVisitCount, int? daysSinceLastVisit, int? daysSinceLastActivity)
    {
        var score = (completedBookingCount * 15) + (upcomingVisitCount * 5);

        score += daysSinceLastVisit switch
        {
            <= 30 => 20,
            <= 90 => 10,
            _ => 0,
        };

        if (daysSinceLastActivity is <= 30)
        {
            score += 10;
        }

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>New (never completed a visit) -&gt; Regular (1-2) -&gt; Loyal (3-5) -&gt; Vip (6+), purely a function of repeat completed visits.</summary>
    private static LoyaltyLevel CalculateLoyaltyLevel(int completedBookingCount) => completedBookingCount switch
    {
        0 => LoyaltyLevel.New,
        <= 2 => LoyaltyLevel.Regular,
        <= 5 => LoyaltyLevel.Loyal,
        _ => LoyaltyLevel.Vip,
    };

    /// <summary>
    /// Any upcoming booking already means Active regardless of how long ago
    /// the last visit was - the customer has already committed to
    /// returning. Otherwise Active/AtRisk/Inactive purely by recency of the
    /// last completed-or-past visit; a customer who has never visited at
    /// all (no <paramref name="daysSinceLastVisit"/>) has shown no
    /// engagement to measure, so falls straight to Inactive rather than a
    /// fourth "unknown" bucket the calling task's 3-value enum doesn't have.
    /// </summary>
    private static EngagementStatus CalculateEngagementStatus(int upcomingVisitCount, int? daysSinceLastVisit)
    {
        if (upcomingVisitCount > 0)
        {
            return EngagementStatus.Active;
        }

        return daysSinceLastVisit switch
        {
            null => EngagementStatus.Inactive,
            <= 60 => EngagementStatus.Active,
            <= 120 => EngagementStatus.AtRisk,
            _ => EngagementStatus.Inactive,
        };
    }
}
