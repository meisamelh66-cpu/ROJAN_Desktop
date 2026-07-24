using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence.Customers;
using Rojan.Desktop.Infrastructure.Persistence.Services;
using Rojan.Desktop.Infrastructure.Persistence.Specialists;

namespace Rojan.Desktop.Infrastructure.Persistence;

/// <summary>
/// Sprint 6 Commit 1 established this as infrastructure plumbing with no
/// <see cref="DbSet{TEntity}"/> at all. Commit 2 added Customers, Commit 3
/// added Specialists, Commit 4 adds Services. Every other Domain module
/// (Bookings/Calendar) still resolves its existing <c>Fake*Repository</c>
/// unchanged - this context only knows about the modules that have
/// actually moved to EF Core so far, added one module at a time, the same
/// cadence Sprint 3/4/5's Rules -&gt; Search/Filter -&gt; Intelligence commits
/// already established.
///
/// EF Core types exist only inside this Infrastructure project -
/// Domain/Application/Presentation never reference
/// <c>Microsoft.EntityFrameworkCore.*</c> directly (enforced by
/// <c>ArchitectureTests.DependencyDirectionTests</c>), consistent with the
/// repository pattern every Domain module already establishes: Domain
/// owns the <c>I*Repository</c> contract, Infrastructure owns whatever
/// concrete storage technology answers it - see
/// <see cref="Customers.CustomerEntity"/>'s own doc comment for why the
/// entity classes below are their own mutable classes rather than mapping
/// a Domain record directly.
/// </summary>
public sealed class RojanDbContext : DbContext
{
    public RojanDbContext(DbContextOptions<RojanDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    public DbSet<CustomerNoteEntity> CustomerNotes => Set<CustomerNoteEntity>();

    public DbSet<CustomerTagEntity> CustomerTags => Set<CustomerTagEntity>();

    public DbSet<CustomerActivityEntity> CustomerActivities => Set<CustomerActivityEntity>();

    public DbSet<SpecialistEntity> Specialists => Set<SpecialistEntity>();

    public DbSet<SpecialistSkillEntity> SpecialistSkills => Set<SpecialistSkillEntity>();

    public DbSet<ServiceEntity> Services => Set<ServiceEntity>();

    public DbSet<SpecialistServiceEntity> SpecialistServices => Set<SpecialistServiceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerNoteEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerTagEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerActivityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpecialistEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpecialistSkillEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpecialistServiceEntityConfiguration());
    }
}
