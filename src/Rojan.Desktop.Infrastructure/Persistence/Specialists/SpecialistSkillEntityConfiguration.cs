using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Specialists;

/// <summary>EF Core mapping for <see cref="SpecialistSkillEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class SpecialistSkillEntityConfiguration : IEntityTypeConfiguration<SpecialistSkillEntity>
{
    public void Configure(EntityTypeBuilder<SpecialistSkillEntity> builder)
    {
        builder.ToTable("SpecialistSkills");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.SpecialistId).IsRequired();
        builder.Property(entity => entity.Name).IsRequired();

        builder.HasIndex(entity => entity.SpecialistId);

        // No navigation property on either side (SpecialistEntity has
        // none either) - the Domain contract treats skills as an
        // independent collection queried by specialist id
        // (ISpecialistRepository.GetSkillsAsync), never eager-loaded as
        // part of Specialist itself, same reasoning
        // Customers.CustomerNoteEntityConfiguration already establishes.
        // Cascade delete: a skill can never outlive the specialist it
        // belongs to.
        builder.HasOne<SpecialistEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.SpecialistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
