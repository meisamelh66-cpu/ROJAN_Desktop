namespace Rojan.Server.Domain.Authentication;

/// <summary>Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Validation rules for <see cref="Organization"/>'s lifecycle - same "small deviation from data-plus-repository-only Domain" reasoning <see cref="UserRules"/> already establishes, and the same shape the desktop solution's own <c>Domain.Customers.CustomerRules</c>/<c>Domain.Specialists.SpecialistRules</c> use for their own status transitions.</summary>
public static class OrganizationRules
{
    private static readonly Dictionary<OrganizationStatus, OrganizationStatus[]> ValidTransitions = new()
    {
        [OrganizationStatus.Active] = [OrganizationStatus.Suspended],
        [OrganizationStatus.Suspended] = [OrganizationStatus.Active],
    };

    /// <summary>Whether moving an organization directly from <paramref name="from"/> to <paramref name="to"/> is legal. Callers must only invoke this for an actual status change (<paramref name="from"/> != <paramref name="to"/>).</summary>
    public static bool IsValidTransition(OrganizationStatus from, OrganizationStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
