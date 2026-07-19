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
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Navigation;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

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

        RegisterModules(services);
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.AddSingleton<MainWindow>();
    }

    /// <summary>
    /// Every module the sidebar shows, in display order. Dashboard (Phase
    /// 06B), Customers (Phase 09/10), Bookings (Phase 11), Calendar (Phase
    /// 14), Specialists (Phase 12), and Services (Phase 13) are the real
    /// modules; the rest are explicitly not built yet (Phase 07 is
    /// architecture-first) and register PlaceholderModule instead of a
    /// bespoke module class - adding their real implementation later is a
    /// one-line swap here, nothing else in the shell changes. Calendar has
    /// no placeholder to swap - it is a genuinely new entry, added the
    /// same way (see CalendarModule's own doc comment).
    /// </summary>
    private static void RegisterModules(IServiceCollection services)
    {
        services.AddSingleton<IModule, DashboardModule>();
        services.AddSingleton<IModule, CustomerModule>();
        services.AddSingleton<IModule, BookingModule>();
        services.AddSingleton<IModule, CalendarModule>();
        services.AddSingleton<IModule, ServiceModule>();
        services.AddSingleton<IModule>(new PlaceholderModule(new ModuleMetadata("inventory", "Inventory", "", 40)));
        services.AddSingleton<IModule>(new PlaceholderModule(new ModuleMetadata("accounting", "Accounting", "", 50)));
        services.AddSingleton<IModule, SpecialistModule>();
        services.AddSingleton<IModule>(new PlaceholderModule(new ModuleMetadata("reports", "Reports", "", 70)));
        services.AddSingleton<IModule>(new PlaceholderModule(new ModuleMetadata("ai-center", "AI Center", "", 80)));
        services.AddSingleton<IModule>(new PlaceholderModule(new ModuleMetadata("settings", "Settings", "", 90)));
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
        MessageBox.Show(
            $"Something went wrong. ROJAN Desktop will try to continue running.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
            "ROJAN Desktop",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
