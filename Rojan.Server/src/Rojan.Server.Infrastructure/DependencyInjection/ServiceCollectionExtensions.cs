using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Application.Tenancy;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Domain.Customers;
using Rojan.Server.Domain.Specialists;
using Rojan.Server.Infrastructure.Persistence;
using Rojan.Server.Infrastructure.Persistence.Repositories;
using Rojan.Server.Infrastructure.Security;

namespace Rojan.Server.Infrastructure.DependencyInjection;

/// <summary>
/// Composition entry point for this layer, same shape as the desktop
/// solution's own <c>Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure</c> -
/// <c>Rojan.Server.Api</c>'s composition root (<c>Program.cs</c>) calls
/// this without needing to know what it registers.
///
/// Sprint 8 Commit 1: Backend Foundation. Registers <see cref="RojanServerDbContext"/>
/// against the <c>ConnectionStrings:DefaultConnection</c> configuration
/// key (populated from <c>appsettings.json</c>/<c>appsettings.Development.json</c>/
/// User Secrets/environment variables - ASP.NET Core's normal
/// configuration precedence, nothing custom here) using the
/// <c>Npgsql.EntityFrameworkCore.PostgreSQL</c> provider.
///
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Adds the
/// authentication repositories (scoped - see <see cref="EfOrganizationRepository"/>'s
/// own doc comment for why scoped rather than the desktop's
/// singleton-plus-factory pattern), <see cref="JwtOptions"/> bound from
/// the <c>Jwt</c> configuration section, and the two security services
/// (<see cref="IPasswordHasher"/>/<see cref="ITokenService"/>) that back
/// <c>Application.Authentication.AuthenticationService</c>. Still no
/// business repositories - there are still no business entities (see
/// <see cref="RojanServerDbContext"/>'s own doc comment).
///
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Adds
/// <see cref="ITenantContext"/> (scoped - see <see cref="ClaimsTenantContext"/>'s
/// own doc comment). <c>services.AddHttpContextAccessor()</c> itself is
/// called from <c>Api.Program</c>, not here - it is available for free on
/// a <c>Microsoft.NET.Sdk.Web</c> project with zero extra package, while
/// this class library needed an explicit
/// <c>Microsoft.AspNetCore.Http.Abstractions</c> reference just for the
/// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> type
/// itself - no reason to duplicate the registration call in two places.
///
/// Sprint 8 Commit 4: Tenant-Aware Customer API. Adds
/// <see cref="ICustomerRepository"/> - the first business-module
/// repository, same scoped-DbContext reasoning as the others.
///
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. Adds
/// <see cref="ISpecialistRepository"/> - the second business-module
/// repository, same scoped-DbContext reasoning as <see cref="ICustomerRepository"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<RojanServerDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();
        services.AddScoped<IBranchRepository, EfBranchRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<ISpecialistRepository, EfSpecialistRepository>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<ITenantContext, ClaimsTenantContext>();

        return services;
    }
}
