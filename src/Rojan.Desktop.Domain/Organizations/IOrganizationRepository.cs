namespace Rojan.Desktop.Domain.Organizations;

/// <summary>
/// Repository abstraction for the Enterprise Multi-Branch platform - the
/// same "dumb, no aggregation logic" shape every other repository in this
/// app follows. <see cref="GetBranchesAsync"/> is the concrete
/// "Repository Filtering" demonstration this phase's test requirements
/// name: it always scopes by <c>organizationId</c>, never returns another
/// organization's branches.
/// </summary>
public interface IOrganizationRepository
{
    public Task<IReadOnlyList<Organization>> GetOrganizationsAsync(CancellationToken cancellationToken = default);

    public Task<Organization?> GetOrganizationByIdAsync(string organizationId, CancellationToken cancellationToken = default);

    public Task<Organization> CreateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default);

    public Task<Organization> UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default);

    /// <summary>Every branch belonging to <paramref name="organizationId"/> - never another organization's, the platform's core data-isolation guarantee.</summary>
    public Task<IReadOnlyList<Branch>> GetBranchesAsync(string organizationId, CancellationToken cancellationToken = default);

    public Task<Branch?> GetBranchByIdAsync(string branchId, CancellationToken cancellationToken = default);

    public Task<Branch> CreateBranchAsync(Branch branch, CancellationToken cancellationToken = default);

    public Task<Branch> UpdateBranchAsync(Branch branch, CancellationToken cancellationToken = default);

    public Task<BranchSettings?> GetBranchSettingsAsync(string branchId, CancellationToken cancellationToken = default);

    public Task<BranchSettings> SetBranchSettingsAsync(BranchSettings settings, CancellationToken cancellationToken = default);
}
