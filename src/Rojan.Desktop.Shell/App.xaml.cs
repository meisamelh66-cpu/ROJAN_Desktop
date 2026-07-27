using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.DependencyInjection;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.DependencyInjection;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Presentation.DependencyInjection;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.Theming;
using Rojan.Desktop.Presentation.Workspaces;
using Rojan.Desktop.Shell.Localization;
using Rojan.Desktop.Shell.Modules;
using Rojan.Desktop.Infrastructure.Automation;
using Rojan.Desktop.Shell.Navigation;
using Rojan.Desktop.Shell.Organizations;
using Rojan.Desktop.Shell.Theming;
using Rojan.Desktop.Shell.Workspaces;

namespace Rojan.Desktop.Shell;

/// <summary>
/// Composition root. Builds the Generic Host, wires every layer's own
/// <c>AddXxx()</c> registration into one declarative list (this class does
/// not itself know how each layer wires its internals - see
/// docs/architecture/01-desktop-shell.md §4), and owns the three .NET
/// unhandled-exception surfaces (§7). Zero business logic - if a change
/// here needs to know a business rule, it belongs in Application/Domain
/// instead.
/// </summary>
public partial class App
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        // Blocking (not awaiting) is deliberate here, not an oversight: this
        // method must stay synchronous end-to-end. WPF's Application.Run()
        // calls OnStartup() and then immediately starts pumping the
        // Dispatcher's main message loop - if OnStartup were async and
        // yielded at an await, control would return to Application.Run()
        // before culture is set, the Dispatcher loop would start pumping
        // against the ExecutionContext baseline captured at that (pre-
        // culture-change) moment, and every later DispatcherOperation
        // would silently replay from that stale baseline - CurrentUICulture
        // reverting to the OS-default mid-session despite having "already"
        // been set. Confirmed by instrumenting Dispatcher.Hooks during
        // Phase 19A: with an async OnStartup, operations posted while
        // CurrentUICulture was correctly fa-IR still started executing
        // under en-US. Blocking here happens before any Dispatcher loop is
        // pumping, so there is no synchronization-context deadlock risk.
        _host.StartAsync().GetAwaiter().GetResult();

        // SQLite startup initialization fix: RojanDbContext's schema was
        // never applied to the real database file anywhere in production -
        // only test fixtures ever called Database.Migrate() (see
        // EfBookingRepositoryTests' own doc comment), leaving the real
        // %LocalAppData%\RojanDesktop\database\rojan.db permanently at
        // zero tables, so every Ef*Repository (Customer/Specialist/
        // Service/Booking/Calendar) failed with "no such table" the
        // instant anything queried it - including the Bookings module,
        // which the persisted default workspace reopens automatically on
        // every launch (root-caused via Windows Event Viewer). Runs first,
        // before every other InitializeAsync below and long before
        // MainWindow/MainWindowViewModel (and thus workspace restoration)
        // are constructed - "the database must exist before anything else
        // can query it," the same "resolve before the thing that reads it"
        // ordering this method already documents throughout. MigrateAsync
        // creates the database file if it does not exist yet and applies
        // every pending migration in one step (never EnsureCreated, which
        // would bypass the migration history this app's own Migrations
        // folder already defines). The factory (not an injected
        // RojanDbContext) matches AddDbContextFactory's own "short-lived
        // context per call, never held" doc comment - created, migrated,
        // and disposed right here.
        using (var dbContext = _host.Services.GetRequiredService<IDbContextFactory<RojanDbContext>>().CreateDbContext())
        {
            dbContext.Database.MigrateAsync().GetAwaiter().GetResult();
        }

        // Fluent 2 Premium Theme pass: the design-system resource tree
        // must be assembled (with the correct Light/Dark color file)
        // before MainWindow or anything else XAML-based is constructed -
        // same "before any XAML" ordering the culture setup below already
        // requires, just for the visual resources instead of strings.
        var themeService = _host.Services.GetRequiredService<IThemeService>();
        themeService.InitializeAsync().GetAwaiter().GetResult();
        ThemeResources.Apply(this, themeService.ResolvedTheme);

        // Phase 19A: language must be resolved and the process culture set
        // before anything XAML-related is constructed - MainWindow (and
        // every {x:Static loc:Strings.Key} reference its tree contains)
        // is built by the very next line, so this has to run first.
        var localizationService = _host.Services.GetRequiredService<ILocalizationService>();
        localizationService.InitializeAsync().GetAwaiter().GetResult();

        var cultureService = _host.Services.GetRequiredService<ICultureService>();
        var culture = cultureService.GetCultureInfo(localizationService.CurrentLanguage.Code);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // Phase 22: the current organization/branch/role must be resolved
        // before MainWindowViewModel builds its permission-filtered
        // NavigationItems - same "before the thing that reads it" ordering
        // as culture/theme above.
        var currentSessionService = _host.Services.GetRequiredService<ICurrentSessionService>();
        currentSessionService.InitializeAsync().GetAwaiter().GetResult();

        // Phase 25: Enterprise Identity & Secure Client Platform. Device/
        // installation registration, session restoration, offline
        // certificate issuance, and sync-queue restoration all need to
        // have run at least once before anything in the app could
        // plausibly ask "who/what is this installation" - none of this
        // gates MainWindow's construction (no UI depends on it yet), so
        // ordering relative to the window itself does not matter, only
        // that every InitializeAsync-shaped call above finishes first
        // (same "resolve before anything reads it" reasoning).
        var deviceRegistrationService = _host.Services.GetRequiredService<IDeviceRegistrationService>();
        deviceRegistrationService.EnsureRegisteredAsync().GetAwaiter().GetResult();

        var sessionService = _host.Services.GetRequiredService<ISessionService>();
        sessionService.InitializeAsync().GetAwaiter().GetResult();

        var certificateService = _host.Services.GetRequiredService<ICertificateService>();
        certificateService.InitializeAsync().GetAwaiter().GetResult();
        if (certificateService.CurrentState == CertificateState.NotIssued)
        {
            certificateService.IssueAsync().GetAwaiter().GetResult();
        }

        var syncQueueService = _host.Services.GetRequiredService<ISyncQueueService>();
        syncQueueService.InitializeAsync().GetAwaiter().GetResult();

        // Phase 27: Enterprise Notification Center. Seeds a realistic
        // starter set covering every severity/priority/category so the
        // Notification Center, badge counter, grouping, search, filtering,
        // and Silent Mode are all genuinely observable on first launch -
        // guarded by "history is currently empty" so this never re-seeds
        // duplicate demo entries on a later restart (the persisted history
        // in %LocalAppData%\RojanDesktop\notifications\history.json is the
        // real, ongoing notification store from this point on).
        var notificationService = _host.Services.GetRequiredService<INotificationService>();
        SeedDemoNotificationsIfEmpty(notificationService);

        // Phase 32: Enterprise Automation, Workflow & Business Rules
        // Engine - starts the Scheduled Jobs background timer (Requirement
        // 32.4). Stopped in OnExit, symmetrically.
        var workflowSchedulerService = _host.Services.GetRequiredService<WorkflowSchedulerService>();
        workflowSchedulerService.Start();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    /// <summary>Phase 27: one-time demo content - see the call site's own doc comment for why this is guarded and where the resulting history actually lives.</summary>
    private static void SeedDemoNotificationsIfEmpty(INotificationService notificationService)
    {
        var existing = notificationService.GetAllAsync().GetAwaiter().GetResult();
        if (existing.Count > 0)
        {
            return;
        }

        var seeds = new[]
        {
            new NotificationRequest(
                NotificationSeverity.Information,
                NotificationPriority.Normal,
                nameof(Strings.Notification_Demo_WelcomeTitle),
                nameof(Strings.Notification_Demo_WelcomeMessage),
                Category: "system"),
            new NotificationRequest(
                NotificationSeverity.Success,
                NotificationPriority.Low,
                nameof(Strings.Notification_Demo_BackupSuccessTitle),
                nameof(Strings.Notification_Demo_BackupSuccessMessage),
                Category: "sync"),
            new NotificationRequest(
                NotificationSeverity.Warning,
                NotificationPriority.High,
                nameof(Strings.Notification_Demo_LowStockTitle),
                nameof(Strings.Notification_Demo_LowStockMessage),
                MessageArgs: ["Aromatherapy Massage Oil"],
                Category: "inventory"),
            new NotificationRequest(
                NotificationSeverity.Error,
                NotificationPriority.Normal,
                nameof(Strings.Notification_Demo_SyncFailedTitle),
                nameof(Strings.Notification_Demo_SyncFailedMessage),
                Category: "sync"),
            new NotificationRequest(
                NotificationSeverity.Information,
                NotificationPriority.Normal,
                nameof(Strings.Notification_Demo_NewBookingTitle),
                nameof(Strings.Notification_Demo_NewBookingMessage),
                MessageArgs: ["Sarah Johnson"],
                Category: "bookings"),
            new NotificationRequest(
                NotificationSeverity.Error,
                NotificationPriority.Critical,
                nameof(Strings.Notification_Demo_SecurityAlertTitle),
                nameof(Strings.Notification_Demo_SecurityAlertMessage),
                Category: "system"),
        };

        foreach (var seed in seeds)
        {
            notificationService.RaiseAsync(seed).GetAwaiter().GetResult();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.Services.GetRequiredService<WorkflowSchedulerService>().Stop();

            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _host.StopAsync(stopTimeout.Token);
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services
            .AddApplication()
            .AddInfrastructure()
            .AddPresentation();

        // Shell owns the concrete navigation implementation - Presentation
        // (and every ViewModel) only ever sees it through INavigationService.
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // Phase 19A: same "interface in Presentation, concrete
        // implementation in Shell" split as Navigation/Dialogs above -
        // these need file-system access (persisted selection, the
        // Languages/ folder) that only the composition root should own.
        services.AddSingleton<ILanguagePackManager, LanguagePackManager>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ILanguagePackRepository, LocalOnlyLanguagePackRepository>();

        // Fluent 2 Premium Theme pass: same "interface in Presentation,
        // concrete implementation in Shell" split as above - ThemeService
        // needs file-system (persisted selection) and Windows-registry
        // (System theme) access, both Shell-level concerns.
        services.AddSingleton<IThemeService, ThemeService>();

        // Phase 22: Enterprise Multi-Branch & Organization Platform - same
        // "interface in Presentation, concrete implementation in Shell"
        // split as Localization/Theming above. Phase 22A: the same
        // instance also serves Application.Organizations.IEnterpriseContext
        // (registered once as itself, aliased to both interfaces) - see
        // CurrentSessionService's own doc comment.
        services.AddSingleton<CurrentSessionService>();
        services.AddSingleton<ICurrentSessionService>(sp => sp.GetRequiredService<CurrentSessionService>());
        services.AddSingleton<IEnterpriseContext>(sp => sp.GetRequiredService<CurrentSessionService>());

        RegisterModules(services);
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();

        // Phase 29: Enterprise Workspace & Window Management - same
        // "interface in Presentation, concrete implementation in Shell"
        // split as Navigation above, since it constructs real Window
        // instances.
        services.AddSingleton<IFloatingWindowManager, FloatingWindowManager>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.AddSingleton<MainWindow>();
    }

    /// <summary>
    /// Every module the sidebar shows, in display order. Dashboard (Phase
    /// 06B), Customers (Phase 09/10), Bookings (Phase 11), Calendar (Phase
    /// 14), Specialists (Phase 12), Services (Phase 13), Inventory (Phase
    /// 17), Accounting (Phase 18), Staff &amp; HR (Phase 19), Settings
    /// (Phase 19A), Reporting/Analytics (Phase 20 - Reporting replaces the
    /// "reports" placeholder one-for-one, Analytics is a genuinely new
    /// entry, same as Calendar/HR before it), and AI Center (Phase 21 -
    /// AiCenterModule replaces the "ai-center" placeholder one-for-one,
    /// the same swap ReportingModule made in Phase 20) are all real
    /// modules now. Every title resolves through <see cref="Strings"/>
    /// (Phase 19A) rather than a literal - the whole sidebar localizes
    /// with the rest of the app.
    /// </summary>
    private static void RegisterModules(IServiceCollection services)
    {
        services.AddSingleton<IModule, DashboardModule>();
        services.AddSingleton<IModule, OrganizationModule>();
        services.AddSingleton<IModule, CustomerModule>();
        services.AddSingleton<IModule, BookingModule>();
        services.AddSingleton<IModule, CalendarModule>();
        services.AddSingleton<IModule, ServiceModule>();
        services.AddSingleton<IModule, InventoryModule>();
        services.AddSingleton<IModule, AccountingModule>();
        services.AddSingleton<IModule, HrModule>();
        services.AddSingleton<IModule, SpecialistModule>();
        services.AddSingleton<IModule, ReportingModule>();
        services.AddSingleton<IModule, AnalyticsModule>();
        services.AddSingleton<IModule, AiCenterModule>();
        services.AddSingleton<IModule, SettingsModule>();
        services.AddSingleton<IModule, AutomationModule>();
        services.AddSingleton<IModule, SupportModule>();
    }

    /// <summary>
    /// UI-thread exceptions: log, show one consistent dialog, then keep the
    /// app running (product decision - favors not losing unsaved user work
    /// over guaranteed-clean state after a UI-thread fault).
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "UI thread");
        ShowErrorDialog(e.Exception);
        e.Handled = true;
    }

    /// <summary>
    /// Non-UI-thread, fatal: the CLR terminates the process after this
    /// handler returns no matter what runs here - this exists only so the
    /// failure is logged before that happens, not to prevent it.
    /// </summary>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogException(exception, "non-UI thread (fatal)");
        }
    }

    /// <summary>Unobserved async/Task faults: log and mark observed so they don't crash the process via the finalizer thread.</summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "unobserved task");
        e.SetObserved();
    }

    private void LogException(Exception exception, string source)
    {
        var logger = _host?.Services.GetService<ILogger<App>>();
        if (logger is not null)
        {
            LogUnhandledException(logger, source, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception ({Source})")]
    private static partial void LogUnhandledException(ILogger logger, string source, Exception exception);

    private static void ShowErrorDialog(Exception exception)
    {
        // "ROJAN Desktop" (both the message text and the dialog title) is
        // the product's brand name, not UI chrome text - left as a
        // literal, same reasoning the header's own brand TextBlock uses,
        // consistent with how brand names are conventionally not
        // translated.
        MessageBox.Show(
            $"{Strings.Common_ErrorDialogMessage}{Environment.NewLine}{Environment.NewLine}{exception.Message}",
            "ROJAN Desktop",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
