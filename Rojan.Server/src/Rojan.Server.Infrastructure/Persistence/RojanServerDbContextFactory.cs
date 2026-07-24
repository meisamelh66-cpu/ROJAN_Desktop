using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rojan.Server.Infrastructure.Persistence;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. Lets `dotnet ef migrations add`/
/// `dotnet ef database update` construct a <see cref="RojanServerDbContext"/>
/// without running the full ASP.NET Core host (design-time tooling has no
/// <c>IServiceProvider</c> to resolve <see cref="RojanServerDbContext"/>
/// from - see <see cref="DependencyInjection.ServiceCollectionExtensions.AddInfrastructure"/>'s
/// own doc comment for the real runtime registration path). Reads the same
/// <c>ConnectionStrings__DefaultConnection</c> environment variable
/// ASP.NET Core's configuration system would bind from
/// <c>ConnectionStrings:DefaultConnection</c> at runtime, falling back to
/// a local-only development default (matching
/// <c>Rojan.Server.Api/appsettings.Development.json</c>'s own placeholder)
/// so the tooling works out of the box against a local PostgreSQL
/// instance without requiring an environment variable to be set first.
///
/// No migration has been generated yet in this commit - there are no
/// entities to migrate (see <see cref="RojanServerDbContext"/>'s own doc
/// comment). This factory only proves the tooling is wired; running
/// `dotnet ef migrations add InitialCreate` is a future commit's job,
/// once real entities exist.
/// </summary>
public sealed class RojanServerDbContextFactory : IDesignTimeDbContextFactory<RojanServerDbContext>
{
    private const string LocalDevelopmentDefault = "Host=localhost;Port=5432;Database=rojan_dev;Username=postgres;Password=postgres";

    public RojanServerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? LocalDevelopmentDefault;

        var optionsBuilder = new DbContextOptionsBuilder<RojanServerDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new RojanServerDbContext(optionsBuilder.Options);
    }
}
