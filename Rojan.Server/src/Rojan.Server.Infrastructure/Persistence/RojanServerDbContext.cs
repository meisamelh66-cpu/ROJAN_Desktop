using Microsoft.EntityFrameworkCore;

namespace Rojan.Server.Infrastructure.Persistence;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. The EF Core/PostgreSQL entry
/// point for this solution - deliberately has zero <see cref="DbSet{TEntity}"/>
/// properties today, since this commit explicitly excludes business
/// entities (see the solution's own README). Exists now so the
/// PostgreSQL/EF Core wiring itself (connection string configuration,
/// <c>Npgsql.EntityFrameworkCore.PostgreSQL</c> provider, `dotnet ef`
/// migration tooling via <see cref="RojanServerDbContextFactory"/>) is
/// proven end-to-end before any entity exists - a future commit adds
/// <c>DbSet</c> properties and its first migration here, not a new
/// context.
/// </summary>
public sealed class RojanServerDbContext : DbContext
{
    public RojanServerDbContext(DbContextOptions<RojanServerDbContext> options)
        : base(options)
    {
    }
}
