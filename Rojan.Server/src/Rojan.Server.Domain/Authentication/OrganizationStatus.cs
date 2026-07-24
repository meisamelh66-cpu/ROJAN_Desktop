namespace Rojan.Server.Domain.Authentication;

/// <summary>Sprint 8 Commit 3: Multi-Tenant Organization Foundation. The lifecycle of an <see cref="Organization"/> - deliberately minimal (two states), since this commit's scope is "strengthen multi-tenant foundation," not build a moderation/billing workflow. <see cref="Suspended"/> exists so tenant access can be denied without deleting the organization's data.</summary>
public enum OrganizationStatus
{
    Active,
    Suspended,
}
