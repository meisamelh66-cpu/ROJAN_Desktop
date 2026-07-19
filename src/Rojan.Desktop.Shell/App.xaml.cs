using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.DependencyInjection;
using Rojan.Desktop.Infrastructure.DependencyInjection;
using Rojan.Desktop.Presentation.DependencyInjection;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Shell.Localization;
using Rojan.Desktop.Shell.Modules;
using Rojan.Desktop.Shell.Navigation;

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

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
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

        RegisterModules(services);
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.AddSingleton<MainWindow>();
    }

    /// <summary>
    /// Every module the sidebar shows, in display order. Dashboard (Phase
    /// 06B), Customers (Phase 09/10), Bookings (Phase 11), Calendar (Phase
    /// 14), Specialists (Phase 12), Services (Phase 13), Inventory (Phase
    /// 17), Accounting (Phase 18), Staff &amp; HR (Phase 19), Settings
    /// (Phase 19A), and Reporting/Analytics (Phase 20 - Reporting replaces
    /// the "reports" placeholder one-for-one, Analytics is a genuinely new
    /// entry, same as Calendar/HR before it) are the real modules; AI
    /// Center is still explicitly not built yet (Phase 07 is
    /// architecture-first) and registers PlaceholderModule instead of a
    /// bespoke module class - adding its real implementation later is a
    /// one-line swap here, nothing else in the shell changes. Every title
    /// resolves through <see cref="Strings"/> (Phase 19A) rather than a
    /// literal - the whole sidebar localizes with the rest of the app.
    /// </summary>
    private static void RegisterModules(IServiceCollection services)
    {
        services.AddSingleton<IModule, DashboardModule>();
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
        // Registered as a factory (not an eagerly-constructed instance) so
        // Strings.Nav_AiCenter evaluates lazily at first resolve (after
        // OnStartup sets CurrentUICulture), matching every other module's
        // static-field-on-first-touch timing - an eager `new
        // PlaceholderModule(...)` here would run during ConfigureServices,
        // before culture is set, freezing this title in whatever the
        // OS-default culture was (the same bug Phase 19A found and fixed
        // for the "reports" placeholder, which no longer exists now that
        // ReportingModule has replaced it).
        services.AddSingleton<IModule>(_ => new PlaceholderModule(new ModuleMetadata("ai-center", Strings.Nav_AiCenter, string.Empty, 80)));
        services.AddSingleton<IModule, SettingsModule>();
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
