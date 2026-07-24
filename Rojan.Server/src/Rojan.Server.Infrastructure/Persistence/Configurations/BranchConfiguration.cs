using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Configurations;

/// <summary>Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. EF Core Fluent API mapping for <see cref="Branch"/>. <see cref="Branch.OrganizationId"/> is a genuine within-tenant parent-child relationship (a branch cannot outlive its organization), so it gets a real foreign key with cascade delete - the same "real FK for genuine parent-child, no FK across a vertical-slice/tenant boundary" reasoning the desktop solution's own EF Core persistence work already establishes.</summary>
public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(branch => branch.Id);

        builder.Property(branch => branch.Id)
            .HasMaxLength(64);

        builder.Property(branch => branch.OrganizationId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(branch => branch.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(branch => branch.CreatedAt)
            .IsRequired();

        builder.HasIndex(branch => branch.OrganizationId);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(branch => branch.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
