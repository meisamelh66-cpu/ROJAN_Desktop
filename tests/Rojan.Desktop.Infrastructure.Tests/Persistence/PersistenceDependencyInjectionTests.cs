using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Infrastructure.DependencyInjection;
using Rojan.Desktop.Infrastructure.Persistence;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises <see cref="ServiceCollectionExtensions.AddInfrastructure"/>'s
/// Sprint 6 Commit 1 persistence registrations specifically - proves the
/// real composition root (not a reimplementation of it) resolves a
/// working <see cref="IDbContextFactory{TContext}"/> and
/// <see cref="SqlitePersistenceOptions"/>, and that adding them did not
/// remove or replace any existing Fake*Repository registration (the
/// "application must continue running unchanged" requirement).
/// </summary>
public sealed class PersistenceDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersSqlitePersistenceOptions()
    {
        var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        var options = provider.GetRequiredService<SqlitePersistenceOptions>();

        Assert.Same(SqlitePersistenceOptions.Default, options);
    }

    [Fact]
    public void AddInfrastructure_RegistersDbContextFactory_AndItProducesAUsableContext()
    {
        var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        var factory = provider.GetRequiredService<IDbContextFactory<RojanDbContext>>();
        using var context = factory.CreateDbContext();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }

    [Fact]
    public void AddInfrastructure_DbContextFactory_IsRegisteredAsSingleton()
    {
        // Matches every other registration in AddInfrastructure - this
        // container has no per-request scope concept (a desktop app, not
        // ASP.NET), so the factory itself (not the short-lived contexts it
        // hands out) must be a singleton, same reasoning the registration's
        // own comment in ServiceCollectionExtensions documents.
        var services = new ServiceCollection().AddInfrastructure();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IDbContextFactory<RojanDbContext>));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_StillRegistersExistingFakeRepositories()
    {
        // The persistence foundation must not replace or remove any
        // existing Fake*Repository registration - Sprint 6 Commit 1 is
        // additive plumbing only.
        var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        Assert.NotNull(provider.GetService<Domain.Customers.ICustomerRepository>());
        Assert.NotNull(provider.GetService<Domain.Specialists.ISpecialistRepository>());
        Assert.NotNull(provider.GetService<Domain.Services.IServiceRepository>());
        Assert.NotNull(provider.GetService<Domain.Bookings.IBookingRepository>());
        Assert.NotNull(provider.GetService<Domain.Calendar.ICalendarRepository>());
    }
}
