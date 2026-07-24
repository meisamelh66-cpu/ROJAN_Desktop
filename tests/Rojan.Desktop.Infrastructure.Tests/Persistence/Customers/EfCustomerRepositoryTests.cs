using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Infrastructure.Persistence.Customers;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence.Customers;

/// <summary>
/// Exercises <see cref="EfCustomerRepository"/> against a real, migrated,
/// temp-file SQLite database - never the production
/// <see cref="SqlitePersistenceOptions.Default"/> path. Every test
/// resolves a fresh <see cref="RojanDbContext"/> per operation through the
/// same <see cref="IDbContextFactory{TContext}"/> the repository itself
/// uses, so a "does it actually persist" assertion means what it says:
/// the data survives a brand new context/connection, not just an
/// in-memory tracked instance from the same context that wrote it.
/// </summary>
public sealed class EfCustomerRepositoryTests : IDisposable
{
    private readonly string _testRoot;
    private readonly EfCustomerRepository _sut;

    public EfCustomerRepositoryTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        var options = new SqlitePersistenceOptions(Path.Combine(_testRoot, "rojan.db"));
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>().UseSqlite(options.ConnectionString);
        var contextFactory = new TestDbContextFactory(optionsBuilder.Options);

        // Applies the real Sprint 6 Commit 2 migration (not EnsureCreated,
        // which would create the schema straight from the model and never
        // actually exercise the migration file itself).
        using var context = contextFactory.CreateDbContext();
        context.Database.Migrate();

        _sut = new EfCustomerRepository(contextFactory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static DomainCustomers.Customer MakeCustomer(
        string id = "customer-1",
        string organizationId = "org-1",
        string branchId = "branch-1",
        DomainCustomers.CustomerStatus status = DomainCustomers.CustomerStatus.Lead) =>
        new(id, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            status, "0", DateTimeOffset.Now, "Notes", organizationId, branchId);

    [Fact]
    public async Task CreateCustomerAsync_ThenGetCustomerByIdAsync_ReturnsThePersistedCustomer()
    {
        var customer = MakeCustomer();

        await _sut.CreateCustomerAsync(customer);
        var found = await _sut.GetCustomerByIdAsync("customer-1");

        Assert.NotNull(found);
        Assert.Equal(customer, found);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_NoMatchingCustomer_ReturnsNull()
    {
        var found = await _sut.GetCustomerByIdAsync("missing-customer");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetCustomersAsync_NoCustomers_ReturnsEmptyList()
    {
        var customers = await _sut.GetCustomersAsync();

        Assert.Empty(customers);
    }

    [Fact]
    public async Task GetCustomersAsync_ReturnsEveryPersistedCustomer()
    {
        await _sut.CreateCustomerAsync(MakeCustomer("customer-1"));
        await _sut.CreateCustomerAsync(MakeCustomer("customer-2"));

        var customers = await _sut.GetCustomersAsync();

        Assert.Equal(2, customers.Count);
        Assert.Contains(customers, customer => customer.Id == "customer-1");
        Assert.Contains(customers, customer => customer.Id == "customer-2");
    }

    [Fact]
    public async Task UpdateCustomerAsync_PersistsEveryChangedField()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        var updated = MakeCustomer(status: DomainCustomers.CustomerStatus.Active) with
        {
            FullName = "Amelia Hart-Ross",
            Company = "Ross Beauty Studio",
            Email = "amelia.ross@example.com",
            Phone = "+1 555 010 9999",
            LifetimeValue = "4,820",
            Notes = "Updated notes.",
        };

        await _sut.UpdateCustomerAsync(updated);
        var found = await _sut.GetCustomerByIdAsync("customer-1");

        Assert.Equal(updated, found);
    }

    [Fact]
    public async Task UpdateCustomerAsync_CustomerDoesNotExist_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateCustomerAsync(MakeCustomer("missing-customer")));
    }

    [Fact]
    public async Task AddNoteAsync_ThenGetNotesAsync_ReturnsThePersistedNote()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        var note = new DomainCustomers.CustomerNote("note-1", "customer-1", "Allergic to certain dyes.", DateTimeOffset.Now);

        await _sut.AddNoteAsync(note);
        var notes = await _sut.GetNotesAsync("customer-1");

        Assert.Equal(note, Assert.Single(notes));
    }

    [Fact]
    public async Task GetNotesAsync_MultipleNotes_ReturnsNewestFirst()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        var now = DateTimeOffset.Now;
        await _sut.AddNoteAsync(new DomainCustomers.CustomerNote("note-older", "customer-1", "Older", now.AddDays(-2)));
        await _sut.AddNoteAsync(new DomainCustomers.CustomerNote("note-newer", "customer-1", "Newer", now));

        var notes = await _sut.GetNotesAsync("customer-1");

        Assert.Equal(["note-newer", "note-older"], notes.Select(note => note.Id));
    }

    [Fact]
    public async Task GetNotesAsync_OnlyMatchingCustomerIdNotesAreReturned()
    {
        await _sut.CreateCustomerAsync(MakeCustomer("customer-1"));
        await _sut.CreateCustomerAsync(MakeCustomer("customer-2"));
        await _sut.AddNoteAsync(new DomainCustomers.CustomerNote("note-1", "customer-1", "For customer 1", DateTimeOffset.Now));
        await _sut.AddNoteAsync(new DomainCustomers.CustomerNote("note-2", "customer-2", "For customer 2", DateTimeOffset.Now));

        var notes = await _sut.GetNotesAsync("customer-1");

        Assert.Equal("note-1", Assert.Single(notes).Id);
    }

    [Fact]
    public async Task AddTagAsync_ThenGetTagsAsync_ReturnsThePersistedTag()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        var tag = new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.Now);

        await _sut.AddTagAsync(tag);
        var tags = await _sut.GetTagsAsync("customer-1");

        Assert.Equal(tag, Assert.Single(tags));
    }

    [Fact]
    public async Task GetTagsAsync_MultipleTags_ReturnsOldestFirst()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        var now = DateTimeOffset.Now;
        await _sut.AddTagAsync(new DomainCustomers.CustomerTag("tag-newer", "customer-1", "Newer", now));
        await _sut.AddTagAsync(new DomainCustomers.CustomerTag("tag-older", "customer-1", "Older", now.AddDays(-2)));

        var tags = await _sut.GetTagsAsync("customer-1");

        Assert.Equal(["tag-older", "tag-newer"], tags.Select(tag => tag.Id));
    }

    [Fact]
    public async Task RemoveTagAsync_RemovesThePersistedTag()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        await _sut.AddTagAsync(new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.Now));

        await _sut.RemoveTagAsync("customer-1", "tag-1");
        var tags = await _sut.GetTagsAsync("customer-1");

        Assert.Empty(tags);
    }

    [Fact]
    public async Task RemoveTagAsync_TagDoesNotExist_DoesNotThrow()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());

        var exception = await Record.ExceptionAsync(() => _sut.RemoveTagAsync("customer-1", "missing-tag"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AddActivityAsync_ThenGetActivityAsync_ReturnsThePersistedActivity()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        var activity = new DomainCustomers.CustomerActivity("activity-1", "customer-1", "Customer created", DateTimeOffset.Now);

        await _sut.AddActivityAsync(activity);
        var activities = await _sut.GetActivityAsync("customer-1");

        Assert.Equal(activity, Assert.Single(activities));
    }

    [Fact]
    public async Task GetActivityAsync_MultipleEntries_ReturnsNewestFirst()
    {
        await _sut.CreateCustomerAsync(MakeCustomer());
        var now = DateTimeOffset.Now;
        await _sut.AddActivityAsync(new DomainCustomers.CustomerActivity("activity-older", "customer-1", "Older", now.AddDays(-2)));
        await _sut.AddActivityAsync(new DomainCustomers.CustomerActivity("activity-newer", "customer-1", "Newer", now));

        var activities = await _sut.GetActivityAsync("customer-1");

        Assert.Equal(["activity-newer", "activity-older"], activities.Select(activity => activity.Id));
    }

    [Fact]
    public async Task GetCustomersAsync_CustomersAcrossMultipleOrganizationsAndBranches_ReturnsAllUnfiltered()
    {
        // Repository-level contract: no Organization/Branch filtering
        // happens here - that stays Application.Customers.CustomerQueryService's
        // job (see ScopeToCurrentSession's own doc comment), so a
        // multi-tenant persisted dataset must come back whole, exactly
        // like FakeCustomerRepository.GetCustomersAsync already behaves.
        await _sut.CreateCustomerAsync(MakeCustomer("customer-1", organizationId: "org-1", branchId: "branch-1"));
        await _sut.CreateCustomerAsync(MakeCustomer("customer-2", organizationId: "org-1", branchId: "branch-2"));
        await _sut.CreateCustomerAsync(MakeCustomer("customer-3", organizationId: "org-2", branchId: "branch-3"));

        var customers = await _sut.GetCustomersAsync();

        Assert.Equal(3, customers.Count);
        Assert.Contains(customers, c => c.OrganizationId == "org-1" && c.BranchId == "branch-1");
        Assert.Contains(customers, c => c.OrganizationId == "org-1" && c.BranchId == "branch-2");
        Assert.Contains(customers, c => c.OrganizationId == "org-2" && c.BranchId == "branch-3");
    }

    [Fact]
    public async Task CreateCustomerAsync_OrganizationAndBranchIdRoundTripExactly()
    {
        await _sut.CreateCustomerAsync(MakeCustomer(organizationId: "org-9", branchId: "branch-42"));

        var found = await _sut.GetCustomerByIdAsync("customer-1");

        Assert.Equal("org-9", found?.OrganizationId);
        Assert.Equal("branch-42", found?.BranchId);
    }

    /// <summary>Minimal <see cref="IDbContextFactory{TContext}"/> for tests - hands out a fresh <see cref="RojanDbContext"/> per call against the same temp-file connection string, same shape <see cref="Rojan.Desktop.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure"/> registers in the running app.</summary>
    private sealed class TestDbContextFactory : IDbContextFactory<RojanDbContext>
    {
        private readonly DbContextOptions<RojanDbContext> _options;

        public TestDbContextFactory(DbContextOptions<RojanDbContext> options)
        {
            _options = options;
        }

        public RojanDbContext CreateDbContext() => new(_options);
    }
}
