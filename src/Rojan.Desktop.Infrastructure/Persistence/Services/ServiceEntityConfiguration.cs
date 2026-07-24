using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Services;

/// <summary>EF Core mapping for <see cref="ServiceEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class ServiceEntityConfiguration : IEntityTypeConfiguration<ServiceEntity>
{
    public void Configure(EntityTypeBuilder<ServiceEntity> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.Name).IsRequired();
        builder.Property(entity => entity.Price).IsRequired();
        builder.Property(entity => entity.Description).IsRequired();

        // ServicePageViewModel's Search & Filter card (Sprint 5 Commit 2)
        // filters by Category and Status - the two repeated read shapes
        // worth indexing, same "index what you actually filter by"
        // reasoning Customers'/Specialists' own indexes already establish.
        builder.HasIndex(entity => entity.Category);
        builder.HasIndex(entity => entity.Status);
    }
}
