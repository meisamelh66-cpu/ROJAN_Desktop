using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Domain.Customers;
using Rojan.Server.Domain.Specialists;

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
///
/// Sprint 8 Commit 4: Tenant-Aware Customer API. Adds
/// <see cref="Customers"/> - the first business-module entity, same
/// direct-record-mapping pattern as the tenant/auth entities above (see
/// <see cref="Configurations.CustomerConfiguration"/>'s own doc comment
/// for why it has no foreign key to <see cref="Organizations"/>/
/// <see cref="Branches"/> despite living in the same database).
///
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. Adds
/// <see cref="Specialists"/> - the second business-module entity, same
/// direct-record-mapping pattern (see
/// <see cref="Configurations.SpecialistConfiguration"/>'s own doc
/// comment).
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

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Specialist> Specialists => Set<Specialist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RojanServerDbContext).Assembly);
    }
}
