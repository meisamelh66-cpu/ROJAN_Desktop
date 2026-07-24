using Rojan.Server.Application.Tenancy;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Tests.Tenancy;

/// <summary>In-memory <see cref="IBranchRepository"/> test double - same reasoning <c>Authentication.FakeAuthenticationRepositories</c> already establishes.</summary>
internal sealed class FakeBranchRepository : IBranchRepository
{
    public List<Branch> Branches { get; } = [];

    public Task<Branch?> GetByIdAsync(string branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Branches.FirstOrDefault(branch => branch.Id == branchId));

    public Task<IReadOnlyList<Branch>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Branch>>(Branches.Where(branch => branch.OrganizationId == organizationId).ToList());
}

/// <summary>Settable <see cref="ITenantContext"/> test double - stands in for <c>Infrastructure.Security.ClaimsTenantContext</c>, whose own claims-reading behavior is covered separately in <c>Infrastructure.Tests.Security.ClaimsTenantContextTests</c>.</summary>
internal sealed class FakeTenantContext : ITenantContext
{
    public string OrganizationId { get; set; } = "org-1";

    public string? BranchId { get; set; }

    public string UserId { get; set; } = "user-1";
}
