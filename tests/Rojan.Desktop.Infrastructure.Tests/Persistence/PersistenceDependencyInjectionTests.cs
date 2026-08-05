using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Infrastructure.DependencyInjection;
using Rojan.Desktop.Infrastructure.Persistence;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises <see cref="Rojan.Desktop.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure"/>'s
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
        // additive plumbing only. IEnterpriseContext is registered here
        // (not by AddInfrastructure itself - the real implementation lives
        // in Shell's composition root) because Owner App Booking
        // Integration's BackendBookingRepository now depends on it - see
        // that class's own doc comment for why (stamping Organization/
        // Branch onto backend-sourced bookings so the existing
        // BookingQueryService.ScopeToCurrentSession filter does not
        // silently discard them). AddApplication() is also needed now -
        // BackendBookingRepository resolves down to HttpApiClient, which
        // needs Application.Security.IRetryPolicy (registered there, not
        // by AddInfrastructure) - matching the real composition root's own
        // AddApplication().AddInfrastructure().AddPresentation() call
        // order (see Shell's App.xaml.cs).
        var services = new ServiceCollection().AddApplication().AddInfrastructure();
        services.AddSingleton<IEnterpriseContext>(new StubEnterpriseContext());
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<Domain.Customers.ICustomerRepository>());
        Assert.NotNull(provider.GetService<Domain.Specialists.ISpecialistRepository>());
        Assert.NotNull(provider.GetService<Domain.Services.IServiceRepository>());
        Assert.NotNull(provider.GetService<Domain.Bookings.IBookingRepository>());
        Assert.NotNull(provider.GetService<Domain.Calendar.ICalendarRepository>());
    }

    [Fact]
    public void AddInfrastructure_RegistersBackendCustomerRepository()
    {
        // Owner App Customer CRM Integration: ICustomerRepository now resolves to the real,
        // backend-connected implementation - same "prove the real composition root, not a
        // reimplementation of it" reasoning as every other assertion in this file.
        var services = new ServiceCollection().AddApplication().AddInfrastructure();
        services.AddSingleton<IEnterpriseContext>(new StubEnterpriseContext());
        var provider = services.BuildServiceProvider();

        var repository = provider.GetRequiredService<Domain.Customers.ICustomerRepository>();

        Assert.IsType<Rojan.Desktop.Infrastructure.Customers.BackendCustomerRepository>(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersBackendServiceRepository()
    {
        // Reception Booking Integration Phase 1: IServiceRepository now resolves to the real,
        // backend-connected implementation - same reasoning as the Customer CRM assertion above.
        var services = new ServiceCollection().AddApplication().AddInfrastructure();
        services.AddSingleton<IEnterpriseContext>(new StubEnterpriseContext());
        var provider = services.BuildServiceProvider();

        var repository = provider.GetRequiredService<Domain.Services.IServiceRepository>();

        Assert.IsType<Rojan.Desktop.Infrastructure.Services.BackendServiceRepository>(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersBackendSpecialistRepository()
    {
        // Reception Booking Integration Phase 2: ISpecialistRepository now resolves to the real,
        // backend-connected implementation - same reasoning as the Customer CRM/Service assertions above.
        var services = new ServiceCollection().AddApplication().AddInfrastructure();
        services.AddSingleton<IEnterpriseContext>(new StubEnterpriseContext());
        var provider = services.BuildServiceProvider();

        var repository = provider.GetRequiredService<Domain.Specialists.ISpecialistRepository>();

        Assert.IsType<Rojan.Desktop.Infrastructure.Specialists.BackendSpecialistRepository>(repository);
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId => "org-1";

        public string? CurrentBranchId => null;

        public WorkspaceRole CurrentRole => WorkspaceRole.OrganizationOwner;
    }
}
