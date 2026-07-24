using Microsoft.EntityFrameworkCore;

namespace Rojan.Desktop.Infrastructure.Persistence;

/// <summary>
/// Sprint 6 Commit 1: EF Core persistence foundation - infrastructure
/// plumbing only, no <see cref="DbSet{TEntity}"/> yet. This commit does
/// not migrate any Domain module (Customers/Bookings/Specialists/
/// Services/Calendar all still resolve their existing <c>Fake*Repository</c>
/// unchanged) - it only establishes the pieces every later per-module
/// commit will build on: this context, <see cref="SqlitePersistenceOptions"/>,
/// and the DI/design-time-tooling registrations around them.
///
/// EF Core types exist only inside this Infrastructure project -
/// Domain/Application/Presentation never reference
/// <c>Microsoft.EntityFrameworkCore.*</c> directly (enforced by
/// <c>ArchitectureTests.DependencyDirectionTests</c>), consistent with the
/// repository pattern every Domain module already establishes: Domain
/// owns the <c>I*Repository</c> contract, Infrastructure owns whatever
/// concrete storage technology answers it. From Commit 2 onward, one
/// module at a time gets a real <see cref="RojanDbContext"/>-backed
/// <c>I*Repository</c> implementation instead of its current fake, each
/// adding its own <see cref="DbSet{TEntity}"/> and entity configuration
/// here - the same one-module-at-a-time cadence Sprint 3/4/5's
/// Rules -&gt; Search/Filter -&gt; Intelligence commits already established.
/// </summary>
public sealed class RojanDbContext : DbContext
{
    public RojanDbContext(DbContextOptions<RojanDbContext> options)
        : base(options)
    {
    }
}
