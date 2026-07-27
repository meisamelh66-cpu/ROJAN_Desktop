using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Specialists;
using Rojan.Server.Infrastructure.Persistence;
using Rojan.Server.Infrastructure.Persistence.Repositories;

namespace Rojan.Server.Infrastructure.Tests.Persistence;

/// <summary>Exercises <see cref="EfSpecialistRepository"/> against EF Core's InMemory provider - same "Infrastructure: persistence / queries scoped correctly" reasoning as <c>EfCustomerRepositoryTests</c>. A real query engine (unlike a hand-rolled fake), so the tenant-scoping <c>WHERE</c> clauses in <see cref="EfSpecialistRepository"/> are genuinely exercised, not just trusted.</summary>
public sealed class EfSpecialistRepositoryTests : IDisposable
{
    private readonly RojanServerDbContext _dbContext;
    private readonly EfSpecialistRepository _sut;

    public EfSpecialistRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<RojanServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        _dbContext = new RojanServerDbContext(options);
        _sut = new EfSpecialistRepository(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private static Specialist NewSpecialist(string id, string organizationId, string? branchId = null) =>
        new(id, organizationId, branchId, "Priya Anand", "555-0100", null, SpecialistStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public async Task CreateAsync_PersistsTheSpecialist()
    {
        await _sut.CreateAsync(NewSpecialist("specialist-1", "org-1"));

        Assert.Single(await _dbContext.Specialists.ToListAsync());
    }

    [Fact]
    public async Task GetByIdAsync_SpecialistBelongsToRequestedOrganization_ReturnsIt()
    {
        await _sut.CreateAsync(NewSpecialist("specialist-1", "org-1"));

        var result = await _sut.GetByIdAsync("org-1", "specialist-1");

        Assert.NotNull(result);
        Assert.Equal("specialist-1", result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_SpecialistBelongsToADifferentOrganization_ReturnsNull()
    {
        // Tenant isolation at the query level: Tenant A's lookup must never
        // return Tenant B's specialist, even by the exact right id.
        await _sut.CreateAsync(NewSpecialist("specialist-1", "org-2"));

        var result = await _sut.GetByIdAsync("org-1", "specialist-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_SpecialistDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("org-1", "missing-specialist");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_ReturnsOnlySpecialistsForThatOrganization()
    {
        await _sut.CreateAsync(NewSpecialist("specialist-1", "org-1"));
        await _sut.CreateAsync(NewSpecialist("specialist-2", "org-1"));
        await _sut.CreateAsync(NewSpecialist("specialist-3", "org-2"));

        var results = await _sut.GetByOrganizationIdAsync("org-1");

        Assert.Equal(2, results.Count);
        Assert.All(results, specialist => Assert.Equal("org-1", specialist.OrganizationId));
    }

    [Fact]
    public async Task UpdateAsync_ExistingSpecialist_PersistsChanges()
    {
        var created = await _sut.CreateAsync(NewSpecialist("specialist-1", "org-1"));

        await _sut.UpdateAsync(created with { FullName = "Updated Name" });

        var reloaded = await _sut.GetByIdAsync("org-1", "specialist-1");
        Assert.Equal("Updated Name", reloaded!.FullName);
    }

    [Fact]
    public async Task UpdateAsync_OrganizationIdDoesNotMatchAnyStoredRow_ThrowsInvalidOperationException()
    {
        // Defense in depth: even if a caller somehow tried to "update" using
        // a tampered OrganizationId, the repository itself must refuse
        // rather than silently writing to the wrong tenant's row (or none).
        await _sut.CreateAsync(NewSpecialist("specialist-1", "org-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(NewSpecialist("specialist-1", "org-2") with { FullName = "Tampered" }));
    }
}
