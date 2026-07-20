namespace Rojan.Desktop.Application.Organizations;

/// <summary>Read-only surface over the Enterprise Multi-Branch platform's own data - organizations, their branches, and each branch's settings. <see cref="GetBranchesAsync"/> is always organization-scoped - the "no cross-branch data leakage" guarantee starts here.</summary>
public interface IOrganizationQueryService
{
    public Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync(CancellationToken cancellationToken = default);

    public Task<OrganizationDto?> GetOrganizationByIdAsync(string organizationId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<BranchDto>> GetBranchesAsync(string organizationId, CancellationToken cancellationToken = default);

    public Task<BranchDto?> GetBranchByIdAsync(string branchId, CancellationToken cancellationToken = default);

    public Task<BranchSettingsDto?> GetBranchSettingsAsync(string branchId, CancellationToken cancellationToken = default);
}
