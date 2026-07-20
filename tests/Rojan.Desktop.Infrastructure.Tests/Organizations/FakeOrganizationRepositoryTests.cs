using Rojan.Desktop.Infrastructure.Organizations;

namespace Rojan.Desktop.Infrastructure.Tests.Organizations;

/// <summary>
/// Exercises <see cref="FakeOrganizationRepository"/>, in particular
/// <see cref="FakeOrganizationRepository.GetBranchesAsync"/> - the
/// concrete "Repository Filtering"/"no cross-branch data leakage"
/// demonstration this phase's spec asks for: a query scoped to one
/// organization must never return another organization's branches.
/// </summary>
public sealed class FakeOrganizationRepositoryTests
{
    [Fact]
    public async Task GetOrganizationsAsync_ReturnsBothSeededOrganizations()
    {
        var repository = new FakeOrganizationRepository();

        var organizations = await repository.GetOrganizationsAsync();

        Assert.Equal(2, organizations.Count);
        Assert.Contains(organizations, o => o.Id == "org-1");
        Assert.Contains(organizations, o => o.Id == "org-2");
    }

    [Fact]
    public async Task GetBranchesAsync_ForOrg1_ReturnsOnlyOrg1Branches()
    {
        var repository = new FakeOrganizationRepository();

        var branches = await repository.GetBranchesAsync("org-1");

        Assert.Equal(2, branches.Count);
        Assert.All(branches, b => Assert.Equal("org-1", b.OrganizationId));
        Assert.DoesNotContain(branches, b => b.Id == "branch-3");
    }

    [Fact]
    public async Task GetBranchesAsync_ForOrg2_ReturnsOnlyOrg2BranchesNotOrg1Branches()
    {
        var repository = new FakeOrganizationRepository();

        var branches = await repository.GetBranchesAsync("org-2");

        Assert.Single(branches);
        Assert.Equal("branch-3", branches[0].Id);
        Assert.All(branches, b => Assert.Equal("org-2", b.OrganizationId));
    }

    [Fact]
    public async Task GetBranchesAsync_ForUnknownOrganization_ReturnsEmpty()
    {
        var repository = new FakeOrganizationRepository();

        var branches = await repository.GetBranchesAsync("org-does-not-exist");

        Assert.Empty(branches);
    }

    [Fact]
    public async Task CreateBranchAsync_ThenGetBranchesAsync_OnlyAffectsItsOwnOrganization()
    {
        var repository = new FakeOrganizationRepository();
        var newBranch = new Domain.Organizations.Branch("branch-99", "org-2", "Extra", "EX-01", "1 Extra St", "+1-555-0000", "extra@example.com", "Sam Reed", "America/Los_Angeles", "USD", Domain.Organizations.BranchStatus.Active);

        await repository.CreateBranchAsync(newBranch);

        var org1Branches = await repository.GetBranchesAsync("org-1");
        var org2Branches = await repository.GetBranchesAsync("org-2");
        Assert.DoesNotContain(org1Branches, b => b.Id == "branch-99");
        Assert.Contains(org2Branches, b => b.Id == "branch-99");
    }

    [Fact]
    public async Task GetBranchSettingsAsync_ReturnsSettingsScopedToThatBranchOnly()
    {
        var repository = new FakeOrganizationRepository();

        var settings = await repository.GetBranchSettingsAsync("branch-1");

        Assert.NotNull(settings);
        Assert.Equal("branch-1", settings!.BranchId);
    }
}
