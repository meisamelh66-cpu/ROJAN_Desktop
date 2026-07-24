using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>EF Core mapping for <see cref="CustomerEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class CustomerEntityConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.FullName).IsRequired();
        builder.Property(entity => entity.Company).IsRequired();
        builder.Property(entity => entity.Email).IsRequired();
        builder.Property(entity => entity.Phone).IsRequired();
        builder.Property(entity => entity.LifetimeValue).IsRequired();
        builder.Property(entity => entity.Notes).IsRequired();
        builder.Property(entity => entity.OrganizationId).IsRequired();
        builder.Property(entity => entity.BranchId).IsRequired();

        // Every meaningful read (Application.Customers.CustomerQueryService.ScopeToCurrentSession)
        // filters by OrganizationId (always) and BranchId (when a branch is
        // selected) - index what is actually filtered by.
        builder.HasIndex(entity => new { entity.OrganizationId, entity.BranchId });
    }
}
