namespace Rojan.Server.Domain.Customers;

/// <summary>Sprint 8 Commit 4: Tenant-Aware Customer API. The lifecycle of a <see cref="Customer"/> - deliberately minimal (two states), matching the same "foundation, not a full workflow" scope <c>Domain.Authentication.OrganizationStatus</c>/<c>BranchStatus</c> already establish for this backend.</summary>
public enum CustomerStatus
{
    Active,
    Inactive,
}
