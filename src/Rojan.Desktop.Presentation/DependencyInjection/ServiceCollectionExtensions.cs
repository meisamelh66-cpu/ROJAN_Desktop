using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Help;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Notifications;
using Rojan.Desktop.Presentation.ViewModels.Accounting;
using Rojan.Desktop.Presentation.ViewModels.AI;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Bookings;
using Rojan.Desktop.Presentation.ViewModels.Calendar;
using Rojan.Desktop.Presentation.ViewModels.Customers;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.HR;
using Rojan.Desktop.Presentation.ViewModels.Analytics;
using Rojan.Desktop.Presentation.ViewModels.Inventory;
using Rojan.Desktop.Presentation.ViewModels.Organizations;
using Rojan.Desktop.Presentation.ViewModels.Reporting;
using Rojan.Desktop.Presentation.ViewModels.Services;
using Rojan.Desktop.Presentation.ViewModels.Security;
using Rojan.Desktop.Presentation.ViewModels.Settings;
using Rojan.Desktop.Presentation.ViewModels.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Support;
using Rojan.Desktop.Presentation.Threading;

namespace Rojan.Desktop.Presentation.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers. Note: the
/// concrete <see cref="Navigation.INavigationService"/> implementation is
/// registered by <c>Shell</c>, not here - this layer only owns the
/// abstraction. Page ViewModels register transient - a fresh instance per
/// navigation, matching docs/architecture/01-desktop-shell.md §4.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Owner App Login Experience: transient, same reasoning as every
        // page ViewModel above - a fresh instance each time the Login
        // window is shown (including after a logout re-triggers it).
        services.AddTransient<LoginViewModel>();

        // Owner App Mobile Login: same transient reasoning as LoginViewModel
        // above. LoginWindowViewModel composes both this and LoginViewModel
        // (see its own doc comment), so it needs the same lifetime.
        services.AddTransient<MobileOtpLoginViewModel>();
        services.AddTransient<LoginWindowViewModel>();
        services.AddSingleton<IDelayScheduler, DispatcherDelayScheduler>();
        services.AddTransient<DashboardPageViewModel>();
        services.AddTransient<CustomerPageViewModel>();
        services.AddTransient<BookingPageViewModel>();
        services.AddTransient<SpecialistPageViewModel>();
        services.AddTransient<ServicePageViewModel>();
        services.AddTransient<CalendarPageViewModel>();
        services.AddTransient<InventoryPageViewModel>();
        services.AddTransient<AccountingPageViewModel>();
        services.AddTransient<HrPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<ReportingPageViewModel>();
        services.AddTransient<AnalyticsPageViewModel>();
        services.AddTransient<AiCenterPageViewModel>();
        services.AddTransient<OrganizationPageViewModel>();
        services.AddTransient<AutomationPageViewModel>();
        services.AddTransient<SupportPageViewModel>();
        services.AddSingleton<ICultureService, CultureService>();
        services.AddSingleton<ICurrencyFormatter, CurrencyFormatter>();

        // Product requirement: centralized DateTime service (foundation
        // commit) - see ICalendarService's own doc comment. Both
        // IDateProvider implementations are now registered (previously
        // only Gregorian was, leaving PersianCalendarProvider unreachable
        // regardless of the active language) so CalendarService can
        // actually select between them.
        services.AddSingleton<IDateProvider, GregorianCalendarProvider>();
        services.AddSingleton<IDateProvider, PersianCalendarProvider>();
        services.AddSingleton<ICalendarService, CalendarService>();

        services.AddSingleton<IHelpContentResolver, HelpContentResolver>();
        services.AddSingleton<INotificationContentResolver, NotificationContentResolver>();
        services.AddSingleton<IToastDismissScheduler, DispatcherToastDismissScheduler>();
        return services;
    }
}
