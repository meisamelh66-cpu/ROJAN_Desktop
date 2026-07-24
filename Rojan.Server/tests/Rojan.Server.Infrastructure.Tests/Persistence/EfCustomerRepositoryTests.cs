using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Customers;
using Rojan.Server.Infrastructure.Persistence;
using Rojan.Server.Infrastructure.Persistence.Repositories;

namespace Rojan.Server.Infrastructure.Tests.Persistence;

/// <summary>Exercises <see cref="EfCustomerRepository"/> against EF Core's InMemory provider - the "Infrastructure: persistence / queries scoped correctly" requirements this commit's own task list calls out explicitly. A real query engine (unlike a hand-rolled fake), so the tenant-scoping <c>WHERE</c> clauses in <see cref="EfCustomerRepository"/> are genuinely exercised, not just trusted.</summary>
public sealed class EfCustomerRepositoryTests : IDisposable
{
    private readonly RojanServerDbContext _dbContext;
    private readonly EfCustomerRepository _sut;

    public EfCustomerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<RojanServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        _dbContext = new RojanServerDbContext(options);
        _sut = new EfCustomerRepository(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private static Customer NewCustomer(string id, string organizationId, string? branchId = null) =>
        new(id, organizationId, branchId, "Noah Bennett", "555-0100", null, CustomerStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public async Task CreateAsync_PersistsTheCustomer()
    {
        await _sut.CreateAsync(NewCustomer("customer-1", "org-1"));

        Assert.Single(await _dbContext.Customers.ToListAsync());
    }

    [Fact]
    public async Task GetByIdAsync_CustomerBelongsToRequestedOrganization_ReturnsIt()
    {
        await _sut.CreateAsync(NewCustomer("customer-1", "org-1"));

        var result = await _sut.GetByIdAsync("org-1", "customer-1");

        Assert.NotNull(result);
        Assert.Equal("customer-1", result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_CustomerBelongsToADifferentOrganization_ReturnsNull()
    {
        // Tenant isolation at the query level: Tenant A's lookup must never
        // return Tenant B's customer, even by the exact right id.
        await _sut.CreateAsync(NewCustomer("customer-1", "org-2"));

        var result = await _sut.GetByIdAsync("org-1", "customer-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_CustomerDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("org-1", "missing-customer");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_ReturnsOnlyCustomersForThatOrganization()
    {
        await _sut.CreateAsync(NewCustomer("customer-1", "org-1"));
        await _sut.CreateAsync(NewCustomer("customer-2", "org-1"));
        await _sut.CreateAsync(NewCustomer("customer-3", "org-2"));

        var results = await _sut.GetByOrganizationIdAsync("org-1");

        Assert.Equal(2, results.Count);
        Assert.All(results, customer => Assert.Equal("org-1", customer.OrganizationId));
    }

    [Fact]
    public async Task UpdateAsync_ExistingCustomer_PersistsChanges()
    {
        var created = await _sut.CreateAsync(NewCustomer("customer-1", "org-1"));

        await _sut.UpdateAsync(created with { Name = "Updated Name" });

        var reloaded = await _sut.GetByIdAsync("org-1", "customer-1");
        Assert.Equal("Updated Name", reloaded!.Name);
    }

    [Fact]
    public async Task UpdateAsync_OrganizationIdDoesNotMatchAnyStoredRow_ThrowsInvalidOperationException()
    {
        // Defense in depth: even if a caller somehow tried to "update" using
        // a tampered OrganizationId, the repository itself must refuse
        // rather than silently writing to the wrong tenant's row (or none).
        await _sut.CreateAsync(NewCustomer("customer-1", "org-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(NewCustomer("customer-1", "org-2") with { Name = "Tampered" }));
    }
}
