using Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Application.Tests.Organizations;

/// <summary>In-memory, mutable <see cref="IOrganizationRepository"/> test double - same shape as <c>Customers.StubCustomerRepository</c>.</summary>
internal sealed class StubOrganizationRepository : IOrganizationRepository
{
    public List<Organization> Organizations { get; } = [];

    public List<Branch> Branches { get; } = [];

    public List<BranchSettings> BranchSettings { get; } = [];

    public Task<IReadOnlyList<Organization>> GetOrganizationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Organization>>(Organizations.ToList());

    public Task<Organization?> GetOrganizationByIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Organizations.FirstOrDefault(o => o.Id == organizationId));

    public Task<Organization> CreateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        Organizations.Add(organization);
        return Task.FromResult(organization);
    }

    public Task<Organization> UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var index = Organizations.FindIndex(o => o.Id == organization.Id);
        Organizations[index] = organization;
        return Task.FromResult(organization);
    }

    public Task<IReadOnlyList<Branch>> GetBranchesAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Branch>>(Branches.Where(b => b.OrganizationId == organizationId).ToList());

    public Task<Branch?> GetBranchByIdAsync(string branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Branches.FirstOrDefault(b => b.Id == branchId));

    public Task<Branch> CreateBranchAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        Branches.Add(branch);
        return Task.FromResult(branch);
    }

    public Task<Branch> UpdateBranchAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        var index = Branches.FindIndex(b => b.Id == branch.Id);
        Branches[index] = branch;
        return Task.FromResult(branch);
    }

    public Task<BranchSettings?> GetBranchSettingsAsync(string branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(BranchSettings.FirstOrDefault(s => s.BranchId == branchId));

    public Task<BranchSettings> SetBranchSettingsAsync(BranchSettings settings, CancellationToken cancellationToken = default)
    {
        var index = BranchSettings.FindIndex(s => s.BranchId == settings.BranchId);
        if (index < 0)
        {
            BranchSettings.Add(settings);
        }
        else
        {
            BranchSettings[index] = settings;
        }

        return Task.FromResult(settings);
    }
}
