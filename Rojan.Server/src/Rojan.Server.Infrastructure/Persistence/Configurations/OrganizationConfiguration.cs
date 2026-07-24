using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Configurations;

/// <summary>Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. EF Core Fluent API mapping for <see cref="Organization"/> - Fluent configuration, not attributes on the Domain record itself (Domain stays free of any EF Core reference at all - see <c>ArchitectureTests</c>' own <c>DependencyDirectionTests</c>, which forbids it at the assembly-reference level).</summary>
public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Id)
            .HasMaxLength(64);

        builder.Property(organization => organization.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(organization => organization.CreatedAt)
            .IsRequired();
    }
}
