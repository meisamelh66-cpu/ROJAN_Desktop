namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Customer Intelligence (Sprint 4 Commit 5): a calculated recency signal -
/// how likely this customer is to still be an active part of the business,
/// derived from upcoming bookings and days since the last visit by
/// <see cref="CustomerInsightsCalculator"/>. Never stored, never a Domain
/// concept - unlike <see cref="CustomerStatus"/> (the customer's actual,
/// persisted lifecycle stage), this is a point-in-time read-model value
/// that can change on every reload without ever writing anything back.
/// </summary>
public enum EngagementStatus
{
    Active,
    AtRisk,
    Inactive,
}
