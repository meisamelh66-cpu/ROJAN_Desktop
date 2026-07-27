namespace Rojan.Server.Domain.Specialists;

/// <summary>Sprint 8 Commit 5: Tenant-Aware Specialist API. The lifecycle of a <see cref="Specialist"/> - deliberately minimal (two states), same "foundation, not a full workflow" scope <c>Domain.Customers.CustomerStatus</c> already establishes for this backend's business modules.</summary>
public enum SpecialistStatus
{
    Active,
    Inactive,
}
