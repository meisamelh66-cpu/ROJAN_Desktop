using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Dashboard;

namespace Rojan.Desktop.Application.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardQueryService, DashboardQueryService>();
        return services;
    }
}
