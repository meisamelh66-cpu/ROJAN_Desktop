namespace Rojan.Server.Domain.Authentication;

/// <summary>Persistence contract for <see cref="Branch"/>. No create/update operations yet - this commit's registration flow never assigns a branch (see <see cref="User.BranchId"/>'s own doc comment), so only read paths exist so far.</summary>
public interface IBranchRepository
{
    public Task<Branch?> GetByIdAsync(string branchId, CancellationToken cancellationToken = default);

    /// <summary>Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Backs <c>Application.Tenancy.ITenantService.GetCurrentOrganizationBranchesAsync</c> - every branch within one tenant, never across organizations.</summary>
    public Task<IReadOnlyList<Branch>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default);
}
