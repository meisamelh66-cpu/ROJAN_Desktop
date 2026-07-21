using DomainOrg = Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Application.Organizations;

public sealed class OrganizationCommandService : IOrganizationCommandService
{
    private readonly DomainOrg.IOrganizationRepository _repository;

    public OrganizationCommandService(DomainOrg.IOrganizationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// TimeZone/Language/Currency are infrastructure-only for this pass
    /// (per this phase's spec: "Only infrastructure is required if UI is
    /// not yet needed") - defaulted here rather than exposed as Create
    /// Organization form fields, same reasoning
    /// <c>OrganizationPageViewModel</c>'s doc comment gives for
    /// <see cref="DomainOrg.Organization.TimeZone"/>/<see cref="DomainOrg.Organization.Language"/>/
    /// <see cref="DomainOrg.Organization.Currency"/>.
    /// </summary>
    public async Task<OrganizationDto> CreateOrganizationAsync(string name, string legalName, string taxInformation, SubscriptionPlan subscription, string code, string phone, string email, string address, CancellationToken cancellationToken = default)
    {
        var organization = new DomainOrg.Organization(
            $"org-{Guid.NewGuid():N}", name, legalName, string.Empty, "#8E28E7", taxInformation,
            OrganizationMapper.MapPlan(subscription), DomainOrg.OrganizationStatus.Trial, DateTimeOffset.Now,
            code, phone, email, address, TimeZoneInfo.Local.Id, "fa-IR", "تومان");

        var created = await _repository.CreateOrganizationAsync(organization, cancellationToken).ConfigureAwait(false);
        return OrganizationMapper.MapOrganization(created);
    }

    public async Task<OrganizationDto> UpdateOrganizationAsync(OrganizationDto organization, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateOrganizationAsync(OrganizationMapper.MapOrganization(organization), cancellationToken).ConfigureAwait(false);
        return OrganizationMapper.MapOrganization(updated);
    }

    public async Task<BranchDto> CreateBranchAsync(string organizationId, string name, string code, string address, string phone, string email, string manager, string timeZone, string currency, CancellationToken cancellationToken = default)
    {
        var branch = new DomainOrg.Branch(
            $"branch-{Guid.NewGuid():N}", organizationId, name, code, address, phone, email, manager, timeZone, currency, DomainOrg.BranchStatus.Active);

        var created = await _repository.CreateBranchAsync(branch, cancellationToken).ConfigureAwait(false);
        return OrganizationMapper.MapBranch(created);
    }

    public async Task<BranchDto> UpdateBranchAsync(BranchDto branch, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateBranchAsync(OrganizationMapper.MapBranch(branch), cancellationToken).ConfigureAwait(false);
        return OrganizationMapper.MapBranch(updated);
    }

    public async Task<BranchSettingsDto> SetBranchSettingsAsync(BranchSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.SetBranchSettingsAsync(OrganizationMapper.MapSettings(settings), cancellationToken).ConfigureAwait(false);
        return OrganizationMapper.MapSettings(saved);
    }
}
