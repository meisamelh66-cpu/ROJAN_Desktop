using Rojan.Server.Application.Tenancy;
using Rojan.Server.Application.Tests.Authentication;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Tests.Tenancy;

/// <summary>Exercises <see cref="TenantService"/>'s access-validation logic against fakes - real claims-to-context resolution is covered separately in <c>Infrastructure.Tests.Security.ClaimsTenantContextTests</c>.</summary>
public sealed class TenantServiceTests
{
    private readonly FakeTenantContext _tenantContext = new();
    private readonly FakeOrganizationRepository _organizationRepository = new();
    private readonly FakeBranchRepository _branchRepository = new();

    private TenantService CreateSut() => new(_tenantContext, _organizationRepository, _branchRepository);

    private static Organization ActiveOrganization(string id = "org-1") =>
        new(id, "Rojan Salon", OrganizationStatus.Active, DateTimeOffset.UtcNow);

    [Fact]
    public async Task GetCurrentTenantAsync_ActiveOrganizationNoBranch_ReturnsExpectedDto()
    {
        _tenantContext.OrganizationId = "org-1";
        _tenantContext.UserId = "user-1";
        _organizationRepository.Organizations.Add(ActiveOrganization());
        var sut = CreateSut();

        var result = await sut.GetCurrentTenantAsync();

        Assert.Equal("org-1", result.OrganizationId);
        Assert.Equal("Rojan Salon", result.OrganizationName);
        Assert.Null(result.BranchId);
        Assert.Null(result.BranchName);
        Assert.Equal("user-1", result.UserId);
    }

    [Fact]
    public async Task GetCurrentTenantAsync_ActiveOrganizationWithActiveBranch_ReturnsBranchInfo()
    {
        _tenantContext.OrganizationId = "org-1";
        _tenantContext.BranchId = "branch-1";
        _organizationRepository.Organizations.Add(ActiveOrganization());
        _branchRepository.Branches.Add(new Branch("branch-1", "org-1", "Downtown", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        var result = await sut.GetCurrentTenantAsync();

        Assert.Equal("branch-1", result.BranchId);
        Assert.Equal("Downtown", result.BranchName);
    }

    [Fact]
    public async Task GetCurrentTenantAsync_OrganizationDoesNotExist_ThrowsTenantAccessDeniedException()
    {
        _tenantContext.OrganizationId = "missing-org";
        var sut = CreateSut();

        await Assert.ThrowsAsync<TenantAccessDeniedException>(() => sut.GetCurrentTenantAsync());
    }

    [Fact]
    public async Task GetCurrentTenantAsync_OrganizationSuspended_ThrowsTenantAccessDeniedException()
    {
        _tenantContext.OrganizationId = "org-1";
        _organizationRepository.Organizations.Add(new Organization("org-1", "Rojan Salon", OrganizationStatus.Suspended, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<TenantAccessDeniedException>(() => sut.GetCurrentTenantAsync());
    }

    [Fact]
    public async Task GetCurrentTenantAsync_BranchDoesNotExist_ThrowsTenantAccessDeniedException()
    {
        _tenantContext.OrganizationId = "org-1";
        _tenantContext.BranchId = "missing-branch";
        _organizationRepository.Organizations.Add(ActiveOrganization());
        var sut = CreateSut();

        await Assert.ThrowsAsync<TenantAccessDeniedException>(() => sut.GetCurrentTenantAsync());
    }

    [Fact]
    public async Task GetCurrentTenantAsync_BranchBelongsToDifferentOrganization_ThrowsTenantAccessDeniedException()
    {
        // The core cross-organization-prevention guarantee, exercised end-to-end through the service.
        _tenantContext.OrganizationId = "org-1";
        _tenantContext.BranchId = "branch-1";
        _organizationRepository.Organizations.Add(ActiveOrganization());
        _branchRepository.Branches.Add(new Branch("branch-1", "org-2", "Someone Else's Branch", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<TenantAccessDeniedException>(() => sut.GetCurrentTenantAsync());
    }

    [Fact]
    public async Task GetCurrentTenantAsync_BranchInactive_ThrowsTenantAccessDeniedException()
    {
        _tenantContext.OrganizationId = "org-1";
        _tenantContext.BranchId = "branch-1";
        _organizationRepository.Organizations.Add(ActiveOrganization());
        _branchRepository.Branches.Add(new Branch("branch-1", "org-1", "Downtown", BranchStatus.Inactive, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<TenantAccessDeniedException>(() => sut.GetCurrentTenantAsync());
    }

    [Fact]
    public async Task GetCurrentOrganizationBranchesAsync_ReturnsOnlyBranchesForTheCurrentOrganization()
    {
        _tenantContext.OrganizationId = "org-1";
        _organizationRepository.Organizations.Add(ActiveOrganization());
        _branchRepository.Branches.Add(new Branch("branch-1", "org-1", "Downtown", BranchStatus.Active, DateTimeOffset.UtcNow));
        _branchRepository.Branches.Add(new Branch("branch-2", "org-1", "Uptown", BranchStatus.Active, DateTimeOffset.UtcNow));
        _branchRepository.Branches.Add(new Branch("branch-3", "org-2", "A Different Tenant's Branch", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        var branches = await sut.GetCurrentOrganizationBranchesAsync();

        Assert.Equal(2, branches.Count);
        Assert.Contains(branches, branch => branch.Id == "branch-1");
        Assert.Contains(branches, branch => branch.Id == "branch-2");
        Assert.DoesNotContain(branches, branch => branch.Id == "branch-3");
    }

    [Fact]
    public async Task ValidateAccessAsync_ValidTenantContext_DoesNotThrow()
    {
        _tenantContext.OrganizationId = "org-1";
        _organizationRepository.Organizations.Add(ActiveOrganization());
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.ValidateAccessAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task ValidateAccessAsync_SuspendedOrganization_ThrowsTenantAccessDeniedException()
    {
        _tenantContext.OrganizationId = "org-1";
        _organizationRepository.Organizations.Add(new Organization("org-1", "Rojan Salon", OrganizationStatus.Suspended, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<TenantAccessDeniedException>(() => sut.ValidateAccessAsync());
    }
}
