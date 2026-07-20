namespace Rojan.Desktop.Application.Organizations;

/// <summary>Write surface for the Enterprise Multi-Branch platform - creating/updating organizations, branches, and branch settings.</summary>
public interface IOrganizationCommandService
{
    public Task<OrganizationDto> CreateOrganizationAsync(string name, string legalName, string taxInformation, SubscriptionPlan subscription, string code, string phone, string email, string address, CancellationToken cancellationToken = default);

    public Task<OrganizationDto> UpdateOrganizationAsync(OrganizationDto organization, CancellationToken cancellationToken = default);

    public Task<BranchDto> CreateBranchAsync(string organizationId, string name, string code, string address, string phone, string email, string manager, string timeZone, string currency, CancellationToken cancellationToken = default);

    public Task<BranchDto> UpdateBranchAsync(BranchDto branch, CancellationToken cancellationToken = default);

    public Task<BranchSettingsDto> SetBranchSettingsAsync(BranchSettingsDto settings, CancellationToken cancellationToken = default);
}
