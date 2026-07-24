using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Application.Support;
using Rojan.Desktop.Domain.Accounting;
using Rojan.Desktop.Domain.AI;
using Rojan.Desktop.Domain.Bookings;
using Rojan.Desktop.Domain.Calendar;
using Rojan.Desktop.Domain.Customers;
using Rojan.Desktop.Domain.Dashboard;
using Rojan.Desktop.Domain.Help;
using Rojan.Desktop.Domain.HR;
using Rojan.Desktop.Domain.Inventory;
using Rojan.Desktop.Domain.Notifications;
using Rojan.Desktop.Domain.Organizations;
using Rojan.Desktop.Domain.Reporting;
using Rojan.Desktop.Domain.Automation;
using Rojan.Desktop.Domain.Specialists;
using Rojan.Desktop.Domain.Support;
using Rojan.Desktop.Domain.Workspaces;
using Rojan.Desktop.Infrastructure.Accounting;
using Rojan.Desktop.Infrastructure.Automation;
using Rojan.Desktop.Infrastructure.AI;
using Rojan.Desktop.Infrastructure.Api;
using Rojan.Desktop.Infrastructure.Calendar;
using Rojan.Desktop.Infrastructure.Connectivity;
using Rojan.Desktop.Infrastructure.Dashboard;
using Rojan.Desktop.Infrastructure.Help;
using Rojan.Desktop.Infrastructure.HR;
using Rojan.Desktop.Infrastructure.Identity;
using Rojan.Desktop.Infrastructure.Inventory;
using Rojan.Desktop.Infrastructure.Notifications;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Infrastructure.Persistence.Bookings;
using Rojan.Desktop.Infrastructure.Persistence.Customers;
using Rojan.Desktop.Infrastructure.Persistence.Specialists;
using Rojan.Desktop.Infrastructure.Reporting;
using Rojan.Desktop.Infrastructure.Search;
using Rojan.Desktop.Infrastructure.Security;
using Rojan.Desktop.Infrastructure.Support;
using Rojan.Desktop.Infrastructure.Sync;
using Rojan.Desktop.Infrastructure.Workspaces;
using DomainServices = Rojan.Desktop.Domain.Services;
using InfraPersistenceServices = Rojan.Desktop.Infrastructure.Persistence.Services;

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

        // Sprint 6 Commit 2: Customers is the first Domain module moved
        // off its Fake*Repository onto EF Core (see RojanDbContext's own
        // doc comment). FakeCustomerRepository itself is intentionally
        // left in the codebase, unreferenced - not deleted - so the
        // previous behavior stays one line away if ever needed again. This
        // is a real, user-visible behavior change: the Customer list now
        // starts empty on a fresh SQLite database instead of showing the
        // fake's 7 seeded demo customers - that is the actual point of
        // "real persistence" (data now genuinely survives a restart,
        // accumulated from real use, not replayed from a hardcoded seed).
        services.AddSingleton<ICustomerRepository, EfCustomerRepository>();

        // Sprint 6 Commit 5: Bookings is the fourth Domain module moved off
        // its Fake*Repository onto EF Core - same reasoning as Customers/
        // Specialists/Services in Commits 2/3/4 (see EfCustomerRepository's
        // own DI comment above). FakeBookingRepository stays in the
        // codebase, unreferenced. No foreign keys to Customers/Specialists/
        // Services (see BookingEntity's own doc comment) - CustomerId/
        // SpecialistId/ServiceId round-trip as plain text, exactly matching
        // Domain.Bookings.Booking's own "free-form, unvalidated references"
        // contract. Behavior change: the Booking list now starts empty on
        // a fresh SQLite database instead of showing the fake's 8 seeded
        // demo bookings.
        services.AddSingleton<IBookingRepository, EfBookingRepository>();

        // Sprint 6 Commit 3: Specialists is the second Domain module moved
        // off its Fake*Repository onto EF Core - same reasoning as
        // Customers in Commit 2 (see EfCustomerRepository's own DI comment
        // above). FakeSpecialistRepository stays in the codebase,
        // unreferenced. Behavior change: the Specialist directory now
        // starts empty on a fresh SQLite database instead of showing the
        // fake's 5 seeded demo specialists.
        services.AddSingleton<ISpecialistRepository, EfSpecialistRepository>();

        // Sprint 6 Commit 4: Services is the third Domain module moved off
        // its Fake*Repository onto EF Core - same reasoning as Customers/
        // Specialists in Commits 2/3 (see EfCustomerRepository's own DI
        // comment above). FakeServiceRepository stays in the codebase,
        // unreferenced. Unlike Customers/Specialists, IServiceRepository
        // has no create/update-service method at all (see
        // EfServiceRepository's own doc comment), so the empty catalog on
        // a fresh database cannot self-heal through the running app the
        // way Customers/Specialists can - a real, known, pre-existing gap
        // (catalog authoring was never in scope for this vertical slice),
        // not something introduced or fixable here.
        services.AddSingleton<DomainServices.IServiceRepository, InfraPersistenceServices.EfServiceRepository>();

        services.AddSingleton<ICalendarRepository, FakeCalendarRepository>();
        services.AddSingleton<IInventoryRepository, FakeInventoryRepository>();
        services.AddSingleton<IAccountingRepository, FakeAccountingRepository>();
        services.AddSingleton<IHrRepository, FakeHrRepository>();
        services.AddSingleton<IReportingRepository, FakeReportingRepository>();
        services.AddSingleton<IAIRepository, FakeAIRepository>();
        services.AddSingleton<IOrganizationRepository, FakeOrganizationRepository>();

        // Sprint 6 Commit 1: EF Core persistence foundation - registered
        // but not yet consumed by any repository (every module above still
        // resolves its Fake*Repository implementation unchanged; see
        // RojanDbContext's own doc comment for the Commit 2+ plan).
        // AddDbContextFactory (not AddDbContext) matches this container's
        // shape: every registration in this method is a long-lived
        // singleton, never a per-request scope (this is a desktop app, not
        // ASP.NET) - the factory itself is registered as a singleton and
        // hands out a short-lived RojanDbContext per call, avoiding both
        // the "scoped service in an all-singleton container" mismatch and
        // DbContext's own not-thread-safe constraint if two singleton
        // repositories ever needed a context concurrently.
        services.AddSingleton(SqlitePersistenceOptions.Default);
        services.AddDbContextFactory<RojanDbContext>(options =>
            options.UseSqlite(SqlitePersistenceOptions.Default.ConnectionString));

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

        // Phase 26: Smart Context Help.
        services.AddSingleton<IHelpRepository, HelpTopicRegistry>();
        services.AddSingleton<IHelpFavoritesStore, LocalHelpFavoritesStore>();
        services.AddSingleton<IHelpRecentlyViewedStore, LocalHelpRecentlyViewedStore>();

        // Phase 27: Enterprise Notification Center.
        services.AddSingleton<INotificationRepository, LocalNotificationRepository>();
        services.AddSingleton<ISilentModePreferenceStore, LocalSilentModePreferenceStore>();

        // Phase 28: Enterprise Global Search & Command Palette.
        services.AddSingleton<ISearchHistoryStore, LocalSearchHistoryStore>();
        services.AddSingleton<ISearchFavoritesStore, LocalSearchFavoritesStore>();

        // Phase 29: Enterprise Workspace & Window Management.
        services.AddSingleton<IWorkspaceRepository, LocalWorkspaceStore>();

        // Phase 32: Enterprise Automation, Workflow & Business Rules
        // Engine. WorkflowSchedulerService is started/stopped explicitly
        // by Shell's composition root (its own Start/Stop, not tied to
        // this container's lifetime) - registered as a singleton here
        // purely so Shell can resolve the one shared instance.
        services.AddSingleton<IWorkflowRepository, LocalWorkflowRepository>();
        services.AddSingleton<IBusinessRuleRepository, LocalBusinessRuleRepository>();
        services.AddSingleton<IScheduledJobRepository, LocalScheduledJobRepository>();
        services.AddSingleton<IApprovalRepository, LocalApprovalRepository>();
        services.AddSingleton<IWorkflowExecutionRepository, LocalWorkflowExecutionRepository>();
        services.AddSingleton<IEmailNotificationService, LocalEmailOutboxService>();
        services.AddSingleton<WorkflowSchedulerService>();

        services.AddSingleton<ISupportMessageRepository, LocalSupportMessageRepository>();
        services.AddSingleton<IDevelopmentApplicationRepository, LocalDevelopmentApplicationRepository>();
        services.AddSingleton<IRojanBrandConfiguration, RojanBrandConfiguration>();

        return services;
    }
}
