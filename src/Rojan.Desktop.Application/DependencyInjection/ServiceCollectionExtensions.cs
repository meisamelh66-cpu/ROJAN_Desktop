using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Application.Specialists;

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
        services.AddSingleton<ICustomerQueryService, CustomerQueryService>();
        services.AddSingleton<ICustomerProfileQueryService, CustomerProfileQueryService>();
        services.AddSingleton<ICustomerCommandService, CustomerCommandService>();
        services.AddSingleton<IBookingQueryService, BookingQueryService>();
        services.AddSingleton<IBookingCommandService, BookingCommandService>();
        services.AddSingleton<ISpecialistQueryService, SpecialistQueryService>();
        services.AddSingleton<ISpecialistProfileQueryService, SpecialistProfileQueryService>();
        services.AddSingleton<ISpecialistCommandService, SpecialistCommandService>();
        return services;
    }
}
