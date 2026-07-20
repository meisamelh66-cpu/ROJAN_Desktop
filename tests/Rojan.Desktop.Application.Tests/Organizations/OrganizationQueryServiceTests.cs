using Rojan.Desktop.Application.Organizations;
using DomainOrg = Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Application.Tests.Organizations;

/// <summary>
/// Exercises <see cref="OrganizationQueryService"/> - specifically
/// Organization Scoping: a query for one organization's branches must
/// never surface another organization's data, the Application-layer half
/// of the same guarantee <c>Infrastructure.Tests.FakeOrganizationRepositoryTests</c>
/// proves at the repository level.
/// </summary>
public sealed class OrganizationQueryServiceTests
{
    private static StubOrganizationRepository SeedTwoOrganizations()
    {
        var repository = new StubOrganizationRepository();
        var now = DateTimeOffset.Now;

        repository.Organizations.Add(new DomainOrg.Organization("org-a", "Org A", "Org A Legal", string.Empty, "#111111", "TIN-A", DomainOrg.SubscriptionPlan.Trial, DomainOrg.OrganizationStatus.Active, now, "OA", "+1-555-0001", "a@example.com", "addr-a", "UTC", "en-US", "USD"));
        repository.Organizations.Add(new DomainOrg.Organization("org-b", "Org B", "Org B Legal", string.Empty, "#222222", "TIN-B", DomainOrg.SubscriptionPlan.Professional, DomainOrg.OrganizationStatus.Active, now, "OB", "+1-555-0002", "b@example.com", "addr-b", "UTC", "en-US", "USD"));

        repository.Branches.Add(new DomainOrg.Branch("branch-a1", "org-a", "A1", "A1", "addr", "phone", "email", "manager", "tz", "USD", DomainOrg.BranchStatus.Active));
        repository.Branches.Add(new DomainOrg.Branch("branch-a2", "org-a", "A2", "A2", "addr", "phone", "email", "manager", "tz", "USD", DomainOrg.BranchStatus.Active));
        repository.Branches.Add(new DomainOrg.Branch("branch-b1", "org-b", "B1", "B1", "addr", "phone", "email", "manager", "tz", "USD", DomainOrg.BranchStatus.Active));

        return repository;
    }

    [Fact]
    public async Task GetBranchesAsync_ForOneOrganization_NeverReturnsAnotherOrganizationsBranches()
    {
        var service = new OrganizationQueryService(SeedTwoOrganizations());

        var branchesForA = await service.GetBranchesAsync("org-a");

        Assert.Equal(2, branchesForA.Count);
        Assert.All(branchesForA, b => Assert.Equal("org-a", b.OrganizationId));
        Assert.DoesNotContain(branchesForA, b => b.Id == "branch-b1");
    }

    [Fact]
    public async Task GetBranchesAsync_ForTheOtherOrganization_ReturnsOnlyItsOwnBranch()
    {
        var service = new OrganizationQueryService(SeedTwoOrganizations());

        var branchesForB = await service.GetBranchesAsync("org-b");

        Assert.Single(branchesForB);
        Assert.Equal("branch-b1", branchesForB[0].Id);
    }

    [Fact]
    public async Task GetOrganizationsAsync_ReturnsEveryOrganizationRegardlessOfScope()
    {
        var service = new OrganizationQueryService(SeedTwoOrganizations());

        var organizations = await service.GetOrganizationsAsync();

        Assert.Equal(2, organizations.Count);
    }

    [Fact]
    public async Task GetOrganizationByIdAsync_UnknownId_ReturnsNull()
    {
        var service = new OrganizationQueryService(SeedTwoOrganizations());

        var organization = await service.GetOrganizationByIdAsync("org-does-not-exist");

        Assert.Null(organization);
    }
}
