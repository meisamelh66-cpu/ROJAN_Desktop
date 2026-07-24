using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Services;

/// <summary>EF Core mapping for <see cref="SpecialistServiceEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class SpecialistServiceEntityConfiguration : IEntityTypeConfiguration<SpecialistServiceEntity>
{
    public void Configure(EntityTypeBuilder<SpecialistServiceEntity> builder)
    {
        builder.ToTable("SpecialistServices");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.ServiceId).IsRequired();
        builder.Property(entity => entity.SpecialistId).IsRequired();
        builder.Property(entity => entity.SpecialistName).IsRequired();

        builder.HasIndex(entity => entity.ServiceId);

        // No navigation property on either side (ServiceEntity has none
        // either) - the Domain contract treats assignments as an
        // independent collection queried by service id
        // (IServiceRepository.GetAssignedSpecialistsAsync), never
        // eager-loaded as part of Service itself, same reasoning
        // Customers.CustomerNoteEntityConfiguration/
        // Specialists.SpecialistSkillEntityConfiguration already
        // establish. SpecialistId/SpecialistName are free-form,
        // unvalidated references (see SpecialistServiceEntity's own doc
        // comment) - no FK to Specialists at all, only to Services, since
        // Domain.Services deliberately does not depend on
        // Domain.Specialists. Cascade delete: an assignment can never
        // outlive the service it belongs to.
        builder.HasOne<ServiceEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
