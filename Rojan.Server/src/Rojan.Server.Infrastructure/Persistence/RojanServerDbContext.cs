using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. The EF Core/PostgreSQL entry
/// point for this solution.
///
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. The first
/// real entities - <see cref="Organizations"/>/<see cref="Branches"/>/
/// <see cref="Users"/>/<see cref="RefreshTokens"/> (Domain records mapped
/// directly via constructor binding, no separate mutable Entity/Mapper
/// pair - unlike the desktop solution's own EF Core persistence, which
/// has good reasons of its own for that extra indirection, this is a
/// fresh backend schema with no equivalent constraint, so mapping the
/// immutable Domain record straight through keeps this commit smaller).
/// Configuration lives entirely in <c>Persistence.Configurations</c>
/// (Fluent API, applied via <c>ModelBuilder.ApplyConfigurationsFromAssembly</c>
/// below) - still no EF Core attribute anywhere in
/// <c>Rojan.Server.Domain</c>.
/// </summary>
public sealed class RojanServerDbContext : DbContext
{
    public RojanServerDbContext(DbContextOptions<RojanServerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RojanServerDbContext).Assembly);
    }
}
