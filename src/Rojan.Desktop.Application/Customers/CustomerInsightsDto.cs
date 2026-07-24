namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Customer Intelligence (Sprint 4 Commit 5): calculated Customer 360
/// insights - every field here is derived on the fly by
/// <see cref="CustomerInsightsCalculator"/> from data the app already has
/// (bookings, activity), never persisted and never duplicated into Domain.
/// Composed into <see cref="CustomerProfileDto"/> alongside
/// <see cref="CustomerBookingSummaryDto"/>, the DTO it's built from, same
/// "calculated, not stored" reasoning <c>CustomerProfileQueryService</c>'s
/// own <c>BuildStatistics</c> already established for the Statistics cards.
/// </summary>
public sealed record CustomerInsightsDto(
    int CustomerScore,
    LoyaltyLevel LoyaltyLevel,
    EngagementStatus EngagementStatus,
    int VisitCount,
    int CompletedVisitCount,
    DateTimeOffset? LastVisitDate,
    int UpcomingVisitCount,
    int? DaysSinceLastVisit)
{
    public static CustomerInsightsDto Empty { get; } = new(0, LoyaltyLevel.New, EngagementStatus.Inactive, 0, 0, null, 0, null);
}
