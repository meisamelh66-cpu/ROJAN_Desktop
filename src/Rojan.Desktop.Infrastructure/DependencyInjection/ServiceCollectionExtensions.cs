using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Application.QrCodes;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Application.Support;
using Rojan.Desktop.Domain.Accounting;
using Rojan.Desktop.Domain.AI;
using Rojan.Desktop.Domain.Bookings;
using Rojan.Desktop.Domain.Customers;
using Rojan.Desktop.Domain.Dashboard;
using Rojan.Desktop.Domain.Help;
using Rojan.Desktop.Domain.HR;
using Rojan.Desktop.Domain.Inventory;
using Rojan.Desktop.Domain.Membership;
using Rojan.Desktop.Domain.Notifications;
using Rojan.Desktop.Domain.Organizations;
using Rojan.Desktop.Domain.Reporting;
using Rojan.Desktop.Domain.Automation;
using Rojan.Desktop.Domain.Salons;
using Rojan.Desktop.Domain.Specialists;
using Rojan.Desktop.Domain.Specialists.Schedule;
using Rojan.Desktop.Domain.Support;
using Rojan.Desktop.Domain.Workspaces;
using Rojan.Desktop.Infrastructure.Accounting;
using Rojan.Desktop.Infrastructure.Automation;
using Rojan.Desktop.Infrastructure.AI;
using Rojan.Desktop.Infrastructure.Api;
using Rojan.Desktop.Infrastructure.Bookings;
using Rojan.Desktop.Infrastructure.Calendar;
using Rojan.Desktop.Infrastructure.Connectivity;
using Rojan.Desktop.Infrastructure.Customers;
using Rojan.Desktop.Infrastructure.Dashboard;
using Rojan.Desktop.Infrastructure.Help;
using Rojan.Desktop.Infrastructure.HR;
using Rojan.Desktop.Infrastructure.Identity;
using Rojan.Desktop.Infrastructure.Inventory;
using Rojan.Desktop.Infrastructure.Membership;
using Rojan.Desktop.Infrastructure.Notifications;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Infrastructure.Persistence.Bookings;
using Rojan.Desktop.Infrastructure.QrCodes;
using Rojan.Desktop.Infrastructure.Reporting;
using Rojan.Desktop.Infrastructure.Salons;
using Rojan.Desktop.Infrastructure.Search;
using Rojan.Desktop.Infrastructure.Security;
using Rojan.Desktop.Infrastructure.Services;
using Rojan.Desktop.Infrastructure.Specialists;
using Rojan.Desktop.Infrastructure.Specialists.Schedule;
using Rojan.Desktop.Infrastructure.Support;
using Rojan.Desktop.Infrastructure.Sync;
using Rojan.Desktop.Infrastructure.Workspaces;
using Rojan.Desktop.Application.Salons;
using AppCalendar = Rojan.Desktop.Application.Calendar;
using DomainServices = Rojan.Desktop.Domain.Services;

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
        // P1-5 - Desktop Observability Foundation: HttpApiClient below now
        // takes an ILogger<HttpApiClient>. Shell's own composition root
        // already gets logging for free from Host.CreateDefaultBuilder(),
        // but this method is also exercised directly against a bare
        // ServiceCollection (see Rojan.Desktop.Infrastructure.Tests'
        // PersistenceDependencyInjectionTests) with no host underneath it.
        // AddLogging() is the standard Microsoft.Extensions.Logging
        // bootstrap - it registers ILoggerFactory/ILogger<T> the same way
        // CreateDefaultBuilder() does internally, and is safe to call even
        // when a host already registered it (no duplicate providers, just
        // guarantees ILogger<T> always resolves for this method's own
        // callers).
        services.AddLogging();

        // Owner App Backend Integration: real GET /api/v1/dashboard/insights,
        // mapped onto the existing KpiMetric/ActivityEntry shapes (see
        // BackendDashboardRepository's own doc comment). FakeDashboardRepository
        // stays in the codebase, unreferenced - same convention as every
        // Sprint 6 Fake->real swap below.
        services.AddSingleton<IDashboardRepository, BackendDashboardRepository>();

        // Owner App Customer CRM Integration: real GET/POST/PATCH/DELETE
        // against ROJAN_Backend's Customer CRM endpoints, replacing
        // EfCustomerRepository (Sprint 6 Commit 2's EF Core swap) - which
        // stays in the codebase, unreferenced, same convention as every
        // earlier Fake/Ef->Backend swap (see BackendDashboardRepository's
        // own DI comment above). FakeCustomerRepository also remains,
        // unreferenced. See BackendCustomerRepository's own doc comment
        // for the mapping honesty notes (UserId-based booking-history
        // linkage, always-present lifetime value, no backend equivalent
        // for the vestigial Notes field/LastContactedAt) and for why
        // AddActivityAsync throws.
        services.AddSingleton<ICustomerRepository, BackendCustomerRepository>();

        // Reception Permission Contract Alignment: the booking-time-only counterpart to
        // ICustomerCommandService.CreateCustomerAsync above - see ICustomerIdentityService's own
        // doc comment for why this is a separate registration, not a variant of the one above.
        services.AddSingleton<ICustomerIdentityService, BackendCustomerIdentityService>();

        // Owner App Booking Integration: real GET/PATCH/PUT against
        // ROJAN_Backend's booking endpoints, replacing EfBookingRepository
        // (Sprint 6 Commit 5's EF Core swap) - which stays in the
        // codebase, unreferenced, same convention as every earlier
        // Fake/Ef->Backend swap (see BackendDashboardRepository's own DI
        // comment above). See BackendBookingRepository's own doc comment
        // for the three deliberate mapping honesty notes (unresolved
        // customer name, best-effort service/specialist name resolution,
        // Organization/Branch stamped from the current session rather than
        // a real backend concept) and for why CreateBookingAsync throws.
        // ISalonContextService is shared by both Customer CRM and Booking
        // integration - registered once, here.
        services.AddSingleton<ISalonContextService, BackendSalonContextService>();

        // Phase 1.2 Owner App Create Salon Flow: no Fake/Ef predecessor to replace -
        // Salon is a genuinely new vertical slice. See BackendSalonRepository's own
        // doc comment for why it depends on IApiClient alone, not ISalonContextService.
        services.AddSingleton<ISalonRepository, BackendSalonRepository>();

        // Reception Production Integration: the real Salon Invite accept flow.
        // ISalonInviteRepository has no Fake/Ef predecessor either, same
        // reasoning as ISalonRepository above. IAcceptedMembershipStore wraps
        // the existing ISecureStorageService (DPAPI) rather than introducing a
        // new persistence mechanism - see its own doc comment.
        services.AddSingleton<ISalonInviteRepository, BackendSalonInviteRepository>();
        services.AddSingleton<IAcceptedMembershipStore, AcceptedMembershipStore>();

        // QR Ecosystem (Desktop Productionization Sprint 1): the one QR
        // type with no backend resource to ask (the Manager app's static
        // download-page link) - see IStaticQrCodeGenerator's own doc
        // comment for why this is Infrastructure-only, never Domain/Application.
        services.AddSingleton<IStaticQrCodeGenerator, QrCoderStaticQrCodeGenerator>();
        services.AddSingleton<IBookingRepository, BackendBookingRepository>();

        // Reception Booking Integration Phase 2 (Specialist Integration):
        // real GET/POST/PUT against ROJAN_Backend's specialist catalog,
        // replacing EfSpecialistRepository (Sprint 6 Commit 3's EF Core
        // swap) - which stays in the codebase, unreferenced, same
        // convention as every earlier Fake/Ef->Backend swap (see
        // BackendDashboardRepository's own DI comment above).
        // FakeSpecialistRepository also remains, unreferenced. See
        // BackendSpecialistRepository's own doc comment for the mapping
        // honesty notes (Title/Email/Phone map to empty, no OnLeave
        // equivalent, why a status change via UpdateSpecialistAsync throws,
        // and why specialist-skill reads/writes are empty/throw).
        services.AddSingleton<ISpecialistRepository, BackendSpecialistRepository>();

        // Phase 7.2.4 Shift Engine (Specialist Schedule) Backend Integration: no Fake
        // predecessor to retain unreferenced - same "genuinely new vertical slice" shape as
        // ISalonRepository above, not a Fake/Ef->Backend swap. See
        // BackendSpecialistScheduleRepository's own doc comment for the full reasoning.
        services.AddSingleton<ISpecialistScheduleRepository, BackendSpecialistScheduleRepository>();

        // Reception Booking Integration Phase 1 (Service Integration): real
        // GET against ROJAN_Backend's category/service catalog, replacing
        // EfServiceRepository (Sprint 6 Commit 4's EF Core swap) - which
        // stays in the codebase, unreferenced, same convention as every
        // earlier Fake/Ef->Backend swap (see BackendDashboardRepository's
        // own DI comment above). FakeServiceRepository also remains,
        // unreferenced. See BackendServiceRepository's own doc comment for
        // the mapping honesty notes (ServiceCategory.Other fallback +
        // CategoryName, no Seasonal equivalent, and why specialist
        // assignment throws) - IServiceRepository still has no create/
        // update-service method at all, so catalog authoring remains out of
        // scope, same pre-existing gap as before this swap.
        services.AddSingleton<DomainServices.IServiceRepository, BackendServiceRepository>();

        // Remediation Phase 3A (Calendar Dead Code Cleanup): the
        // ICalendarRepository -> EfCalendarRepository registration that used to sit here (Sprint 6
        // Commit 6's EF Core swap) was removed entirely, not merely left unreferenced like its
        // Customer/Specialist/Service/Booking siblings above - verified to have zero production
        // callers once ICalendarCommandService (its sole Application-layer consumer) was also
        // removed, since availability reads never depended on it (see
        // BackendCalendarAvailabilityRepository's own doc comment) and real booking creation
        // stopped orchestrating a Calendar reserve/release step once Backend became the sole
        // booking-conflict authority (BookingWorkflowService's own "Governance correction" comment).
        // ICalendarRepository (Domain), FakeCalendarRepository, and CalendarQueryService (the
        // unregistered, local/EF-independent generation logic) all remain, deliberately - they
        // still serve CalendarQueryService's own retained test coverage. See
        // ROJAN_DESKTOP_CALENDAR_CLEANUP_PHASE3A_REPORT_v1.md for the full verification trail.

        // Calendar/Availability Integration Phase 3: ICalendarQueryService
        // (Application) is registered here instead of in AddApplication() -
        // BackendCalendarAvailabilityRepository implements it directly,
        // deliberately bypassing CalendarQueryService/ICalendarRepository's
        // "raw schedule rows in, generate slots in Application" shape,
        // since ROJAN_Backend's available-slots endpoint already performs
        // that entire computation server-side. CalendarQueryService stays
        // in the codebase, unreferenced, same convention as every earlier
        // Fake/Ef->Backend swap. See BackendCalendarAvailabilityRepository's
        // own doc comment for the full reasoning and its honesty notes.
        services.AddSingleton<AppCalendar.ICalendarQueryService, BackendCalendarAvailabilityRepository>();

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

        // Owner App Backend Integration: real POST /api/v1/auth/login +
        // /auth/refresh via AuthBootstrapHttpClient (deliberately not
        // IApiClient - see that class's own doc comment), DPAPI-encrypted
        // session persistence via the ISecureStorageService already
        // registered above (closes the plaintext-token-storage finding
        // from the Owner App Integration Audit). LocalSessionService/
        // LocalAuthenticationService stay in the codebase, unreferenced -
        // same convention as every Sprint 6 Fake->real swap above.
        // Owner App Login Experience: Development/Production base-address
        // selection (persisted, restart-required) - see IApiEnvironmentService's
        // own doc comment. Registered ahead of AuthBootstrapHttpClient/
        // HttpApiClient since both now depend on it to resolve their base
        // address instead of reading ROJAN_API_BASE_URL directly.
        services.AddSingleton<IApiEnvironmentService, ApiEnvironmentService>();
        services.AddSingleton<AuthBootstrapHttpClient>();
        services.AddSingleton<ISessionService, BackendSessionService>();
        services.AddSingleton<IAuthenticationService, BackendAuthenticationService>();
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
