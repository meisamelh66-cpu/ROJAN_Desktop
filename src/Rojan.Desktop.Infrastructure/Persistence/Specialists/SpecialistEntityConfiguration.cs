using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Specialists;

/// <summary>EF Core mapping for <see cref="SpecialistEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class SpecialistEntityConfiguration : IEntityTypeConfiguration<SpecialistEntity>
{
    public void Configure(EntityTypeBuilder<SpecialistEntity> builder)
    {
        builder.ToTable("Specialists");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.FullName).IsRequired();
        builder.Property(entity => entity.Title).IsRequired();
        builder.Property(entity => entity.Email).IsRequired();
        builder.Property(entity => entity.Phone).IsRequired();
        builder.Property(entity => entity.Bio).IsRequired();

        // SpecialistPageViewModel's status filter (Sprint 5 Commit 4) is
        // the one repeated read shape worth indexing - no
        // OrganizationId/BranchId to compose it with, unlike Customers'
        // index, since Specialist has no such columns at all.
        builder.HasIndex(entity => entity.Status);
    }
}
