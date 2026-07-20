using DomainOrg = Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Application.Organizations;

public sealed class OrganizationQueryService : IOrganizationQueryService
{
    private readonly DomainOrg.IOrganizationRepository _repository;

    public OrganizationQueryService(DomainOrg.IOrganizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        var organizations = await _repository.GetOrganizationsAsync(cancellationToken).ConfigureAwait(false);
        return organizations.Select(OrganizationMapper.MapOrganization).ToList();
    }

    public async Task<OrganizationDto?> GetOrganizationByIdAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var organization = await _repository.GetOrganizationByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return organization is null ? null : OrganizationMapper.MapOrganization(organization);
    }

    public async Task<IReadOnlyList<BranchDto>> GetBranchesAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var branches = await _repository.GetBranchesAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return branches.Select(OrganizationMapper.MapBranch).ToList();
    }

    public async Task<BranchDto?> GetBranchByIdAsync(string branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _repository.GetBranchByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        return branch is null ? null : OrganizationMapper.MapBranch(branch);
    }

    public async Task<BranchSettingsDto?> GetBranchSettingsAsync(string branchId, CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetBranchSettingsAsync(branchId, cancellationToken).ConfigureAwait(false);
        return settings is null ? null : OrganizationMapper.MapSettings(settings);
    }
}
