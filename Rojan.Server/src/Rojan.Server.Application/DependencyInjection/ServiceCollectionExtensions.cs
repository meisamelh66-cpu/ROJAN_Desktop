using Microsoft.Extensions.DependencyInjection;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Application.Tenancy;

namespace Rojan.Server.Application.DependencyInjection;

/// <summary>
/// Composition entry point for this layer, same shape as the desktop
/// solution's own <c>Application.DependencyInjection.ServiceCollectionExtensions.AddApplication</c> -
/// <c>Rojan.Server.Api</c>'s composition root calls this without knowing
/// what, if anything, it registers.
///
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation.
/// <see cref="IAuthenticationService"/> is the first real registration
/// here - its own dependencies (<see cref="IPasswordHasher"/>/
/// <see cref="ITokenService"/>/the repositories) are Infrastructure
/// concerns registered by <c>Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure</c>
/// instead, the same "interface here, concrete implementation resolved
/// from the outer layer at composition time" split every other module in
/// this codebase already uses.
///
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Adds
/// <see cref="ITenantService"/> - scoped, since it depends on
/// <see cref="ITenantContext"/>, which is itself scoped to one HTTP
/// request (see <c>Infrastructure.Security.ClaimsTenantContext</c>'s own
/// doc comment).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITenantService, TenantService>();

        return services;
    }
}
