namespace Rojan.Desktop.Application.Organizations;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Organization-level writes require <see cref="Permission.OrganizationManage"/>; branch-level writes require <see cref="Permission.BranchManage"/>.</summary>
public sealed class OrganizationCommandServicePermissionGate : IOrganizationCommandService
{
    private readonly IOrganizationCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public OrganizationCommandServicePermissionGate(IOrganizationCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<OrganizationDto> CreateOrganizationAsync(string name, string legalName, string taxInformation, SubscriptionPlan subscription, string code, string phone, string email, string address, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.OrganizationManage);
        return _inner.CreateOrganizationAsync(name, legalName, taxInformation, subscription, code, phone, email, address, cancellationToken);
    }

    public Task<OrganizationDto> UpdateOrganizationAsync(OrganizationDto organization, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.OrganizationManage);
        return _inner.UpdateOrganizationAsync(organization, cancellationToken);
    }

    public Task<BranchDto> CreateBranchAsync(string organizationId, string name, string code, string address, string phone, string email, string manager, string timeZone, string currency, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BranchManage);
        return _inner.CreateBranchAsync(organizationId, name, code, address, phone, email, manager, timeZone, currency, cancellationToken);
    }

    public Task<BranchDto> UpdateBranchAsync(BranchDto branch, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BranchManage);
        return _inner.UpdateBranchAsync(branch, cancellationToken);
    }

    public Task<BranchSettingsDto> SetBranchSettingsAsync(BranchSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.BranchManage);
        return _inner.SetBranchSettingsAsync(settings, cancellationToken);
    }
}
