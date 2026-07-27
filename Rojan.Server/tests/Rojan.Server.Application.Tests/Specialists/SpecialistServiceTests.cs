using Rojan.Server.Application.Specialists;
using Rojan.Server.Application.Tests.Tenancy;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Domain.Specialists;

namespace Rojan.Server.Application.Tests.Specialists;

/// <summary>Exercises <see cref="SpecialistService"/> - including the "Application: tenant isolation / cross-organization access denied" requirements, same as <c>Customers.CustomerServiceTests</c>.</summary>
public sealed class SpecialistServiceTests
{
    private readonly FakeTenantContext _tenantContext = new();
    private readonly FakeSpecialistRepository _specialistRepository = new();
    private readonly FakeBranchRepository _branchRepository = new();

    private SpecialistService CreateSut() => new(_tenantContext, _specialistRepository, _branchRepository);

    private static CreateSpecialistRequest NewCreateRequest(string? branchId = null) =>
        new("Priya Anand", "555-0100", "priya@rojan.example", branchId);

    [Fact]
    public async Task CreateSpecialistAsync_ValidRequest_CreatesSpecialistScopedToCurrentOrganization()
    {
        _tenantContext.OrganizationId = "org-1";
        var sut = CreateSut();

        var result = await sut.CreateSpecialistAsync(NewCreateRequest());

        var stored = Assert.Single(_specialistRepository.Specialists);
        Assert.Equal("org-1", stored.OrganizationId);
        Assert.Equal("Priya Anand", stored.FullName);
        Assert.Equal("555-0100", stored.Phone);
        Assert.Equal("priya@rojan.example", stored.Email);
        Assert.Equal(SpecialistStatus.Active, stored.Status);
        Assert.Equal(stored.Id, result.Id);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task CreateSpecialistAsync_WithBranchBelongingToOwnOrganization_Succeeds()
    {
        _tenantContext.OrganizationId = "org-1";
        _branchRepository.Branches.Add(new Branch("branch-1", "org-1", "Downtown", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        var result = await sut.CreateSpecialistAsync(NewCreateRequest("branch-1"));

        Assert.Equal("branch-1", result.BranchId);
    }

    [Fact]
    public async Task CreateSpecialistAsync_WithBranchBelongingToDifferentOrganization_ThrowsInvalidSpecialistBranchException()
    {
        _tenantContext.OrganizationId = "org-1";
        _branchRepository.Branches.Add(new Branch("branch-1", "org-2", "Someone Else's Branch", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidSpecialistBranchException>(() => sut.CreateSpecialistAsync(NewCreateRequest("branch-1")));

        Assert.Empty(_specialistRepository.Specialists);
    }

    [Fact]
    public async Task CreateSpecialistAsync_WithNonexistentBranch_ThrowsInvalidSpecialistBranchException()
    {
        _tenantContext.OrganizationId = "org-1";
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidSpecialistBranchException>(() => sut.CreateSpecialistAsync(NewCreateRequest("missing-branch")));
    }

    [Fact]
    public async Task GetSpecialistAsync_SpecialistBelongsToCurrentOrganization_ReturnsDto()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _specialistRepository.Specialists.Add(new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Active, now, now));
        var sut = CreateSut();

        var result = await sut.GetSpecialistAsync("specialist-1");

        Assert.Equal("specialist-1", result.Id);
        Assert.Equal("Priya Anand", result.FullName);
    }

    [Fact]
    public async Task GetSpecialistAsync_SpecialistBelongsToDifferentOrganization_ThrowsSpecialistNotFoundException()
    {
        // The core tenant-isolation guarantee: Tenant A must never be able to read Tenant B's specialist.
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _specialistRepository.Specialists.Add(new Specialist("specialist-1", "org-2", null, "Someone Else's Specialist", "555-0200", null, SpecialistStatus.Active, now, now));
        var sut = CreateSut();

        await Assert.ThrowsAsync<SpecialistNotFoundException>(() => sut.GetSpecialistAsync("specialist-1"));
    }

    [Fact]
    public async Task GetSpecialistAsync_SpecialistDoesNotExistAtAll_ThrowsSpecialistNotFoundException()
    {
        _tenantContext.OrganizationId = "org-1";
        var sut = CreateSut();

        await Assert.ThrowsAsync<SpecialistNotFoundException>(() => sut.GetSpecialistAsync("missing-specialist"));
    }

    [Fact]
    public async Task GetSpecialistsAsync_ReturnsOnlySpecialistsForTheCurrentOrganization()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _specialistRepository.Specialists.Add(new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Active, now, now));
        _specialistRepository.Specialists.Add(new Specialist("specialist-2", "org-1", null, "Ava Chen", "555-0101", null, SpecialistStatus.Active, now, now));
        _specialistRepository.Specialists.Add(new Specialist("specialist-3", "org-2", null, "A Different Tenant's Specialist", "555-0300", null, SpecialistStatus.Active, now, now));
        var sut = CreateSut();

        var results = await sut.GetSpecialistsAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, specialist => specialist.Id == "specialist-1");
        Assert.Contains(results, specialist => specialist.Id == "specialist-2");
        Assert.DoesNotContain(results, specialist => specialist.Id == "specialist-3");
    }

    private static UpdateSpecialistRequest NewUpdateRequest(string status = "Active", string? branchId = null) =>
        new("Priya Anand-Sharma", "555-0199", "priya.updated@rojan.example", branchId, status);

    [Fact]
    public async Task UpdateSpecialistAsync_ValidRequest_UpdatesFieldsAndPreservesOrganizationId()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _specialistRepository.Specialists.Add(new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Active, now, now));
        var sut = CreateSut();

        var result = await sut.UpdateSpecialistAsync("specialist-1", NewUpdateRequest());

        Assert.Equal("Priya Anand-Sharma", result.FullName);
        Assert.Equal("555-0199", result.Phone);
        Assert.Equal("priya.updated@rojan.example", result.Email);
        Assert.Equal("org-1", Assert.Single(_specialistRepository.Specialists).OrganizationId);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_SpecialistBelongsToDifferentOrganization_ThrowsSpecialistNotFoundExceptionAndChangesNothing()
    {
        // Cross-organization access denied - Tenant A must not be able to modify Tenant B's specialist,
        // and must get the same "not found" response as if it never existed.
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        var foreignSpecialist = new Specialist("specialist-1", "org-2", null, "Someone Else's Specialist", "555-0200", null, SpecialistStatus.Active, now, now);
        _specialistRepository.Specialists.Add(foreignSpecialist);
        var sut = CreateSut();

        await Assert.ThrowsAsync<SpecialistNotFoundException>(() => sut.UpdateSpecialistAsync("specialist-1", NewUpdateRequest()));

        Assert.Equal(foreignSpecialist, Assert.Single(_specialistRepository.Specialists));
    }

    [Fact]
    public async Task UpdateSpecialistAsync_UnrecognizedStatus_ThrowsInvalidSpecialistStatusException()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _specialistRepository.Specialists.Add(new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Active, now, now));
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidSpecialistStatusException>(() => sut.UpdateSpecialistAsync("specialist-1", NewUpdateRequest(status: "NotARealStatus")));
    }

    [Fact]
    public async Task UpdateSpecialistAsync_SameStatus_DoesNotThrowAndUpdatesOtherFields()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _specialistRepository.Specialists.Add(new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Active, now, now));
        var sut = CreateSut();

        var result = await sut.UpdateSpecialistAsync("specialist-1", NewUpdateRequest(status: "Active"));

        Assert.Equal("Priya Anand-Sharma", result.FullName);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_WithBranchBelongingToDifferentOrganization_ThrowsInvalidSpecialistBranchException()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _specialistRepository.Specialists.Add(new Specialist("specialist-1", "org-1", null, "Priya Anand", "555-0100", null, SpecialistStatus.Active, now, now));
        _branchRepository.Branches.Add(new Branch("branch-1", "org-2", "Someone Else's Branch", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidSpecialistBranchException>(() => sut.UpdateSpecialistAsync("specialist-1", NewUpdateRequest(branchId: "branch-1")));
    }
}
