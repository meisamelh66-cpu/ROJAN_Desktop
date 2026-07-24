using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>EF Core mapping for <see cref="CustomerNoteEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class CustomerNoteEntityConfiguration : IEntityTypeConfiguration<CustomerNoteEntity>
{
    public void Configure(EntityTypeBuilder<CustomerNoteEntity> builder)
    {
        builder.ToTable("CustomerNotes");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.CustomerId).IsRequired();
        builder.Property(entity => entity.Text).IsRequired();

        builder.HasIndex(entity => entity.CustomerId);

        // No navigation property on either side (CustomerEntity has none
        // either) - the Domain contract treats notes as an independent
        // collection queried by customer id
        // (ICustomerRepository.GetNotesAsync), never eager-loaded as part
        // of Customer itself, so the entity shapes mirror that. Cascade
        // delete: a note can never outlive the customer it belongs to.
        builder.HasOne<CustomerEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
