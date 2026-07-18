using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

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
        return services;
    }
}
