namespace Rojan.Server.Application.Tenancy;

/// <summary>
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Tenant-aware
/// application service - everything it does is scoped to whatever
/// <see cref="ITenantContext"/> resolves for the current request. No
/// business module service (Customer/Specialist/Service/Booking) - those
/// are explicitly out of this commit's scope; this only covers the
/// organization/branch tenant shell itself.
/// </summary>
public interface ITenantService
{
    /// <summary>Resolves the current tenant, throwing <see cref="TenantAccessDeniedException"/> if the organization/branch the request claims does not check out (does not exist, is suspended/inactive, or the branch does not belong to the organization).</summary>
    public Task<CurrentTenantDto> GetCurrentTenantAsync(CancellationToken cancellationToken = default);

    /// <summary>Every branch within the current organization - see <see cref="GetCurrentTenantAsync"/> for the same access validation this performs first.</summary>
    public Task<IReadOnlyList<BranchSummaryDto>> GetCurrentOrganizationBranchesAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates the current request's tenant context without returning anything - for a caller that only needs to know "is this request's tenant usable," not the tenant's details.</summary>
    public Task ValidateAccessAsync(CancellationToken cancellationToken = default);
}
