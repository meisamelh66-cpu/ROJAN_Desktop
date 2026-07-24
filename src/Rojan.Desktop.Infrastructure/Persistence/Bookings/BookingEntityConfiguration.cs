using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Bookings;

/// <summary>
/// EF Core mapping for <see cref="BookingEntity"/> - applied by
/// <see cref="RojanDbContext.OnModelCreating"/>. No
/// <c>HasOne()/HasForeignKey()</c> to Customers/Specialists/Services here -
/// see <see cref="BookingEntity"/>'s own doc comment for why that would be
/// a real architecture violation, not an oversight.
/// </summary>
internal sealed class BookingEntityConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.CustomerId).IsRequired();
        builder.Property(entity => entity.CustomerName).IsRequired();
        builder.Property(entity => entity.ServiceId).IsRequired();
        builder.Property(entity => entity.ServiceName).IsRequired();
        builder.Property(entity => entity.SpecialistId).IsRequired();
        builder.Property(entity => entity.SpecialistName).IsRequired();
        builder.Property(entity => entity.Price).IsRequired();
        builder.Property(entity => entity.Notes).IsRequired();
        builder.Property(entity => entity.OrganizationId).IsRequired();
        builder.Property(entity => entity.BranchId).IsRequired();

        // Every meaningful read (Application.Bookings.BookingQueryService.ScopeToCurrentSession)
        // filters by OrganizationId (always) and BranchId (when selected) -
        // same reasoning Customers.CustomerEntityConfiguration's own index
        // already establishes. CustomerId/SpecialistId/ServiceId are
        // plain (non-FK) lookups Application composes over
        // (Customers.CustomerProfileQueryService filters bookings by
        // CustomerId, BookingCommandService's double-booking guard filters
        // by SpecialistId, catalog-facing reads filter by ServiceId) -
        // indexed for that read pattern even without a foreign key.
        // Status is filtered by BookingSearchFilter and the double-booking
        // guard's "active bookings only" check.
        builder.HasIndex(entity => new { entity.OrganizationId, entity.BranchId });
        builder.HasIndex(entity => entity.CustomerId);
        builder.HasIndex(entity => entity.SpecialistId);
        builder.HasIndex(entity => entity.ServiceId);
        builder.HasIndex(entity => entity.Status);
    }
}
