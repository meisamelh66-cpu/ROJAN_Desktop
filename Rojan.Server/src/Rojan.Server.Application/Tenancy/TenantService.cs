using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Tenancy;

/// <summary>
/// Default <see cref="ITenantService"/>. <see cref="LoadAndValidateAsync"/>
/// is the one place the actual tenant-access rule lives - every public
/// method routes through it first, so "does this request's tenant context
/// still check out" is answered exactly once, the same way, everywhere:
/// the organization must exist and be <see cref="Organization.IsActive"/>,
/// and if a branch is claimed it must exist, actually belong to that
/// organization (<see cref="UserRules.IsValidBranchAssignment"/> - the
/// same rule that prevents cross-organization assignment in the first
/// place), and be <see cref="Branch.IsActive"/>.
/// </summary>
public sealed class TenantService : ITenantService
{
    private readonly ITenantContext _tenantContext;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBranchRepository _branchRepository;

    public TenantService(ITenantContext tenantContext, IOrganizationRepository organizationRepository, IBranchRepository branchRepository)
    {
        _tenantContext = tenantContext;
        _organizationRepository = organizationRepository;
        _branchRepository = branchRepository;
    }

    public async Task<CurrentTenantDto> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var (organization, branch) = await LoadAndValidateAsync(cancellationToken).ConfigureAwait(true);

        return new CurrentTenantDto(organization.Id, organization.Name, branch?.Id, branch?.Name, _tenantContext.UserId);
    }

    public async Task<IReadOnlyList<BranchSummaryDto>> GetCurrentOrganizationBranchesAsync(CancellationToken cancellationToken = default)
    {
        await LoadAndValidateAsync(cancellationToken).ConfigureAwait(true);

        var branches = await _branchRepository.GetByOrganizationIdAsync(_tenantContext.OrganizationId, cancellationToken).ConfigureAwait(true);

        return branches.Select(branch => new BranchSummaryDto(branch.Id, branch.Name)).ToList();
    }

    public async Task ValidateAccessAsync(CancellationToken cancellationToken = default) =>
        await LoadAndValidateAsync(cancellationToken).ConfigureAwait(true);

    private async Task<(Organization Organization, Branch? Branch)> LoadAndValidateAsync(CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(_tenantContext.OrganizationId, cancellationToken).ConfigureAwait(true)
            ?? throw new TenantAccessDeniedException("The organization for this session no longer exists.");

        if (!organization.IsActive)
        {
            throw new TenantAccessDeniedException("The organization for this session is suspended.");
        }

        Branch? branch = null;
        if (_tenantContext.BranchId is not null)
        {
            branch = await _branchRepository.GetByIdAsync(_tenantContext.BranchId, cancellationToken).ConfigureAwait(true);
            if (branch is null || !UserRules.IsValidBranchAssignment(organization.Id, branch))
            {
                throw new TenantAccessDeniedException("The branch for this session does not belong to this organization.");
            }

            if (!branch.IsActive)
            {
                throw new TenantAccessDeniedException("The branch for this session is inactive.");
            }
        }

        return (organization, branch);
    }
}
