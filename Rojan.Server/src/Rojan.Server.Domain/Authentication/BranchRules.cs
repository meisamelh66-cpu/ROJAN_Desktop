namespace Rojan.Server.Domain.Authentication;

/// <summary>Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Validation rules for <see cref="Branch"/>'s lifecycle - same shape as <see cref="OrganizationRules"/>.</summary>
public static class BranchRules
{
    private static readonly Dictionary<BranchStatus, BranchStatus[]> ValidTransitions = new()
    {
        [BranchStatus.Active] = [BranchStatus.Inactive],
        [BranchStatus.Inactive] = [BranchStatus.Active],
    };

    /// <summary>Whether moving a branch directly from <paramref name="from"/> to <paramref name="to"/> is legal. Callers must only invoke this for an actual status change (<paramref name="from"/> != <paramref name="to"/>).</summary>
    public static bool IsValidTransition(BranchStatus from, BranchStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
