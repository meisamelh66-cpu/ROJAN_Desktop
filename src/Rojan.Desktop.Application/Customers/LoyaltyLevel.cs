namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Customer Intelligence (Sprint 4 Commit 5): a calculated relationship
/// tier, entirely derived from <see cref="CustomerBookingSummaryDto.CompletedBookingCount"/>
/// by <see cref="CustomerInsightsCalculator"/> - never stored, never a
/// Domain concept (unlike <see cref="CustomerStatus"/>, which the customer
/// record actually persists). Ordered low-to-high; see
/// <see cref="CustomerInsightsCalculator"/> for the exact thresholds.
/// </summary>
public enum LoyaltyLevel
{
    New,
    Regular,
    Loyal,
    Vip,
}
