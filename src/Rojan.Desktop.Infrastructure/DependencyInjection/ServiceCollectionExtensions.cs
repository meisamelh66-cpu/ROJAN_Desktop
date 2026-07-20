using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Accounting;
using Rojan.Desktop.Domain.AI;
using Rojan.Desktop.Domain.Bookings;
using Rojan.Desktop.Domain.Calendar;
using Rojan.Desktop.Domain.Customers;
using Rojan.Desktop.Domain.Dashboard;
using Rojan.Desktop.Domain.HR;
using Rojan.Desktop.Domain.Inventory;
using Rojan.Desktop.Domain.Organizations;
using Rojan.Desktop.Domain.Reporting;
using Rojan.Desktop.Domain.Specialists;
using Rojan.Desktop.Infrastructure.Accounting;
using Rojan.Desktop.Infrastructure.AI;
using Rojan.Desktop.Infrastructure.Api;
using Rojan.Desktop.Infrastructure.Bookings;
using Rojan.Desktop.Infrastructure.Calendar;
using Rojan.Desktop.Infrastructure.Connectivity;
using Rojan.Desktop.Infrastructure.Customers;
using Rojan.Desktop.Infrastructure.Dashboard;
using Rojan.Desktop.Infrastructure.HR;
using Rojan.Desktop.Infrastructure.Identity;
using Rojan.Desktop.Infrastructure.Inventory;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Infrastructure.Reporting;
using Rojan.Desktop.Infrastructure.Security;
using Rojan.Desktop.Infrastructure.Specialists;
using Rojan.Desktop.Infrastructure.Sync;
using DomainServices = Rojan.Desktop.Domain.Services;
using InfraServices = Rojan.Desktop.Infrastructure.Services;

namespace Rojan.Desktop.Infrastructure.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers. The
/// Services vertical slice is aliased to avoid any visual confusion with
/// <see cref="IServiceCollection"/>/<see cref="ServiceCollectionExtensions"/>
/// in this same file - same names, unrelated concepts.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardRepository, FakeDashboardRepository>();
        services.AddSingleton<ICustomerRepository, FakeCustomerRepository>();
        services.AddSingleton<IBookingRepository, FakeBookingRepository>();
        services.AddSingleton<ISpecialistRepository, FakeSpecialistRepository>();
        services.AddSingleton<DomainServices.IServiceRepository, InfraServices.FakeServiceRepository>();
        services.AddSingleton<ICalendarRepository, FakeCalendarRepository>();
        services.AddSingleton<IInventoryRepository, FakeInventoryRepository>();
        services.AddSingleton<IAccountingRepository, FakeAccountingRepository>();
        services.AddSingleton<IHrRepository, FakeHrRepository>();
        services.AddSingleton<IReportingRepository, FakeReportingRepository>();
        services.AddSingleton<IAIRepository, FakeAIRepository>();
        services.AddSingleton<IOrganizationRepository, FakeOrganizationRepository>();

        // Phase 25: Enterprise Identity & Secure Client Platform.
        // Registration order mirrors the dependency chain: Identity ->
        // secure storage/keys/encryption -> session/certificate/auth ->
        // connectivity/sync/api - every dependency below a given
        // registration is itself registered somewhere in this method, so
        // the whole graph resolves without a Service Locator anywhere.
        services.AddSingleton<IDeviceRegistrationService, DeviceRegistrationService>();
        services.AddSingleton<IIdentityContextService, IdentityContextService>();

        services.AddSingleton<ISecureStorageService, DpapiSecureStorageService>();
        services.AddSingleton<IKeyProvider, LocalKeyProvider>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddSingleton<ISecretProvider, SecretProvider>();

        services.AddSingleton<ISessionService, LocalSessionService>();
        services.AddSingleton<IAuthenticationService, LocalAuthenticationService>();
        services.AddSingleton<ICertificateService, LocalCertificateService>();

        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IApiClient, HttpApiClient>();
        services.AddSingleton<ISyncQueueService, SyncQueueService>();

        return services;
    }
}
