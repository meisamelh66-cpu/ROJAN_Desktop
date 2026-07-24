using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>EF Core mapping for <see cref="CustomerActivityEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class CustomerActivityEntityConfiguration : IEntityTypeConfiguration<CustomerActivityEntity>
{
    public void Configure(EntityTypeBuilder<CustomerActivityEntity> builder)
    {
        builder.ToTable("CustomerActivities");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.CustomerId).IsRequired();
        builder.Property(entity => entity.Description).IsRequired();

        builder.HasIndex(entity => entity.CustomerId);

        // See CustomerNoteEntityConfiguration's own comment - same "no
        // navigation property, cascade delete" reasoning applies here.
        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
