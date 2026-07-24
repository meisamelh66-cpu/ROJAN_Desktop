using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Infrastructure.Persistence.Services;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence.Services;

/// <summary>
/// Exercises <see cref="EfServiceRepository"/> against a real, migrated,
/// temp-file SQLite database - never the production
/// <see cref="SqlitePersistenceOptions.Default"/> path. Same shape as
/// <c>Customers.EfCustomerRepositoryTests</c>/<c>Specialists.EfSpecialistRepositoryTests</c>.
///
/// Unlike those two, <see cref="DomainServices.IServiceRepository"/> has
/// no create/update-service method at all (see <see cref="EfServiceRepository"/>'s
/// own doc comment), so there is no "CreateServiceAsync persists" test
/// here to write - every test seeds its service rows directly through a
/// <see cref="RojanDbContext"/> (bypassing the repository entirely for
/// arrange, exactly mirroring how the real database will only ever be
/// populated - by a future migration/import, never by this repository)
/// and then exercises only the methods the contract actually has:
/// <see cref="DomainServices.IServiceRepository.GetServicesAsync"/>,
/// <see cref="DomainServices.IServiceRepository.GetServiceByIdAsync"/>,
/// and the specialist-assignment methods it does own. No Organization/
/// Branch tests either - like <c>Domain.Specialists.Specialist</c>,
/// <see cref="DomainServices.Service"/> has no such fields (confirmed by
/// reading the Domain record and both Application services before writing
/// any code).
/// </summary>
public sealed class EfServiceRepositoryTests : IDisposable
{
    private readonly string _testRoot;
    private readonly TestDbContextFactory _contextFactory;
    private readonly EfServiceRepository _sut;

    public EfServiceRepositoryTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        var options = new SqlitePersistenceOptions(Path.Combine(_testRoot, "rojan.db"));
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>().UseSqlite(options.ConnectionString);
        _contextFactory = new TestDbContextFactory(optionsBuilder.Options);

        // Applies the real Sprint 6 Commit 4 migration (not EnsureCreated,
        // which would create the schema straight from the model and never
        // actually exercise the migration file itself).
        using var context = _contextFactory.CreateDbContext();
        context.Database.Migrate();

        _sut = new EfServiceRepository(_contextFactory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static DomainServices.Service MakeService(
        string id = "service-1",
        DomainServices.ServiceCategory category = DomainServices.ServiceCategory.Hair,
        DomainServices.ServiceStatus status = DomainServices.ServiceStatus.Active,
        int durationMinutes = 60,
        string price = "80") =>
        new(id, "Haircut & Style", category, status, durationMinutes, price, "Classic cut and blow-dry finish.");

    /// <summary>Arrange-only seeding, bypassing the repository entirely - see this class's own doc comment for why (no create method exists on the contract).</summary>
    private async Task SeedServiceAsync(DomainServices.Service service)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Services.Add(ServiceEntityMapper.MapToEntity(service));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetServicesAsync_NoServices_ReturnsEmptyList()
    {
        var services = await _sut.GetServicesAsync();

        Assert.Empty(services);
    }

    [Fact]
    public async Task GetServicesAsync_ReturnsEverySeededService()
    {
        await SeedServiceAsync(MakeService("service-1"));
        await SeedServiceAsync(MakeService("service-2"));

        var services = await _sut.GetServicesAsync();

        Assert.Equal(2, services.Count);
        Assert.Contains(services, service => service.Id == "service-1");
        Assert.Contains(services, service => service.Id == "service-2");
    }

    [Fact]
    public async Task GetServiceByIdAsync_ExistingService_ReturnsTheSeededService()
    {
        var service = MakeService();
        await SeedServiceAsync(service);

        var found = await _sut.GetServiceByIdAsync("service-1");

        Assert.NotNull(found);
        Assert.Equal(service, found);
    }

    [Fact]
    public async Task GetServiceByIdAsync_NoMatchingService_ReturnsNull()
    {
        var found = await _sut.GetServiceByIdAsync("missing-service");

        Assert.Null(found);
    }

    [Theory]
    [InlineData(DomainServices.ServiceCategory.Hair)]
    [InlineData(DomainServices.ServiceCategory.Colour)]
    [InlineData(DomainServices.ServiceCategory.Nails)]
    [InlineData(DomainServices.ServiceCategory.Skin)]
    [InlineData(DomainServices.ServiceCategory.Spa)]
    [InlineData(DomainServices.ServiceCategory.Consultation)]
    public async Task GetServiceByIdAsync_EveryCategory_RoundTripsExactly(DomainServices.ServiceCategory category)
    {
        await SeedServiceAsync(MakeService(category: category));

        var found = await _sut.GetServiceByIdAsync("service-1");

        Assert.Equal(category, found?.Category);
    }

    [Theory]
    [InlineData(DomainServices.ServiceStatus.Active)]
    [InlineData(DomainServices.ServiceStatus.Seasonal)]
    [InlineData(DomainServices.ServiceStatus.Discontinued)]
    public async Task GetServiceByIdAsync_EveryStatus_RoundTripsExactly(DomainServices.ServiceStatus status)
    {
        await SeedServiceAsync(MakeService(status: status));

        var found = await _sut.GetServiceByIdAsync("service-1");

        Assert.Equal(status, found?.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(150)]
    public async Task GetServiceByIdAsync_DurationMinutes_RoundTripsExactly(int durationMinutes)
    {
        await SeedServiceAsync(MakeService(durationMinutes: durationMinutes));

        var found = await _sut.GetServiceByIdAsync("service-1");

        Assert.Equal(durationMinutes, found?.DurationMinutes);
    }

    [Theory]
    [InlineData("80")]
    [InlineData("1,200,000 تومان")]
    [InlineData("رایگان")]
    public async Task GetServiceByIdAsync_Price_RoundTripsExactly(string price)
    {
        await SeedServiceAsync(MakeService(price: price));

        var found = await _sut.GetServiceByIdAsync("service-1");

        Assert.Equal(price, found?.Price);
    }

    [Fact]
    public async Task AssignSpecialistAsync_ThenGetAssignedSpecialistsAsync_ReturnsThePersistedAssignment()
    {
        await SeedServiceAsync(MakeService());
        var assignment = new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee");

        await _sut.AssignSpecialistAsync(assignment);
        var assignments = await _sut.GetAssignedSpecialistsAsync("service-1");

        Assert.Equal(assignment, Assert.Single(assignments));
    }

    [Fact]
    public async Task GetAssignedSpecialistsAsync_MultipleAssignments_ReturnsEveryPersistedAssignment()
    {
        await SeedServiceAsync(MakeService());
        await _sut.AssignSpecialistAsync(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee"));
        await _sut.AssignSpecialistAsync(new DomainServices.SpecialistService("assignment-2", "service-1", "specialist-2", "Casey Morgan"));

        var assignments = await _sut.GetAssignedSpecialistsAsync("service-1");

        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, assignment => assignment.SpecialistId == "specialist-1");
        Assert.Contains(assignments, assignment => assignment.SpecialistId == "specialist-2");
    }

    [Fact]
    public async Task GetAssignedSpecialistsAsync_OnlyMatchingServiceIdAssignmentsAreReturned()
    {
        await SeedServiceAsync(MakeService("service-1"));
        await SeedServiceAsync(MakeService("service-2"));
        await _sut.AssignSpecialistAsync(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "For service 1"));
        await _sut.AssignSpecialistAsync(new DomainServices.SpecialistService("assignment-2", "service-2", "specialist-1", "For service 2"));

        var assignments = await _sut.GetAssignedSpecialistsAsync("service-1");

        Assert.Equal("assignment-1", Assert.Single(assignments).Id);
    }

    [Fact]
    public async Task UnassignSpecialistAsync_RemovesThePersistedAssignment()
    {
        await SeedServiceAsync(MakeService());
        await _sut.AssignSpecialistAsync(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee"));

        await _sut.UnassignSpecialistAsync("service-1", "assignment-1");
        var assignments = await _sut.GetAssignedSpecialistsAsync("service-1");

        Assert.Empty(assignments);
    }

    [Fact]
    public async Task UnassignSpecialistAsync_OnlyRemovesTheMatchingAssignmentNeverAffectsOtherServices()
    {
        await SeedServiceAsync(MakeService("service-1"));
        await SeedServiceAsync(MakeService("service-2"));
        await _sut.AssignSpecialistAsync(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee"));
        await _sut.AssignSpecialistAsync(new DomainServices.SpecialistService("assignment-2", "service-2", "specialist-1", "Jordan Lee"));

        await _sut.UnassignSpecialistAsync("service-1", "assignment-1");

        Assert.Empty(await _sut.GetAssignedSpecialistsAsync("service-1"));
        Assert.Single(await _sut.GetAssignedSpecialistsAsync("service-2"));
    }

    [Fact]
    public async Task UnassignSpecialistAsync_AssignmentDoesNotExist_DoesNotThrow()
    {
        await SeedServiceAsync(MakeService());

        var exception = await Record.ExceptionAsync(() => _sut.UnassignSpecialistAsync("service-1", "missing-assignment"));

        Assert.Null(exception);
    }

    /// <summary>Minimal <see cref="IDbContextFactory{TContext}"/> for tests - hands out a fresh <see cref="RojanDbContext"/> per call against the same temp-file connection string, same shape <see cref="Rojan.Desktop.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure"/> registers in the running app. Also used directly by this test class's own seeding helper, since the repository itself has no create method.</summary>
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
