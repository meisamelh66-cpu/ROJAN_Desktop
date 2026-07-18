using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Domain.Customers;
using Rojan.Desktop.Domain.Dashboard;
using Rojan.Desktop.Infrastructure.Customers;
using Rojan.Desktop.Infrastructure.Dashboard;

namespace Rojan.Desktop.Infrastructure.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardRepository, FakeDashboardRepository>();
        services.AddSingleton<ICustomerRepository, FakeCustomerRepository>();
        return services;
    }
}
