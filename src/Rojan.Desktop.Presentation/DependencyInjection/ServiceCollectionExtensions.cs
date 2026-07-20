using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Help;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Notifications;
using Rojan.Desktop.Presentation.ViewModels.Accounting;
using Rojan.Desktop.Presentation.ViewModels.AI;
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
using Rojan.Desktop.Presentation.ViewModels.Settings;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

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
        services.AddSingleton<ICultureService, CultureService>();
        services.AddSingleton<ICurrencyFormatter, CurrencyFormatter>();
        services.AddSingleton<IDateProvider, GregorianCalendarProvider>();
        services.AddSingleton<IHelpContentResolver, HelpContentResolver>();
        services.AddSingleton<INotificationContentResolver, NotificationContentResolver>();
        services.AddSingleton<IToastDismissScheduler, DispatcherToastDismissScheduler>();
        return services;
    }
}
