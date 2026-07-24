using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Server.Infrastructure.Persistence;

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
/// <c>Npgsql.EntityFrameworkCore.PostgreSQL</c> provider. No other
/// infrastructure exists yet - this commit is EF Core/PostgreSQL wiring
/// only, no business repositories (there are no business entities to
/// have a repository for - see <see cref="RojanServerDbContext"/>'s own
/// doc comment).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<RojanServerDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
