using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence.Bookings;
using Rojan.Desktop.Infrastructure.Persistence.Customers;
using Rojan.Desktop.Infrastructure.Persistence.Services;
using Rojan.Desktop.Infrastructure.Persistence.Specialists;

namespace Rojan.Desktop.Infrastructure.Persistence;

/// <summary>
/// Sprint 6 Commit 1 established this as infrastructure plumbing with no
/// <see cref="DbSet{TEntity}"/> at all. Commit 2 added Customers, Commit 3
/// added Specialists, Commit 4 added Services, Commit 5 added Bookings,
/// Commit 6 added Calendar - every Domain module with real persistence at
/// the time, added one module at a time, the same cadence Sprint 3/4/5's
/// Rules -&gt; Search/Filter -&gt; Intelligence commits already established.
///
/// Remediation Phase 3A (Calendar Dead Code Cleanup): Commit 6's Calendar
/// entities (<c>WorkingScheduleEntity</c>/<c>WorkingScheduleBreakEntity</c>/
/// <c>ReservedSlotEntity</c>) and their <see cref="DbSet{TEntity}"/>
/// properties/configuration registrations were removed from this context -
/// confirmed to have zero production callers (see
/// ROJAN_DESKTOP_CALENDAR_CLEANUP_PHASE3A_REPORT_v1.md). The migration that
/// created their tables (<c>20260724145626_AddCalendarPersistence</c>) and
/// the model snapshot were deliberately left unchanged - EF Core's snapshot
/// references entity types by string name, not a live C# reference, so this
/// removal does not affect the migration history's own compilability; a
/// fresh local database will still create these (now permanently unused)
/// tables until a future, explicit decommissioning migration is added. Not
/// done here - out of this cleanup's own scope, and not a behavior change
/// either way since nothing reads or writes them regardless.
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

    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

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
        modelBuilder.ApplyConfiguration(new BookingEntityConfiguration());
    }
}
