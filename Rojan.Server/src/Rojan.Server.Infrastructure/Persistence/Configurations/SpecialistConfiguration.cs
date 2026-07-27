using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rojan.Server.Domain.Specialists;

namespace Rojan.Server.Infrastructure.Persistence.Configurations;

/// <summary>
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. EF Core Fluent API
/// mapping for <see cref="Specialist"/> - Domain record mapped directly
/// via constructor binding, same pattern <c>Configurations.CustomerConfiguration</c>
/// already establishes for this backend (see
/// <see cref="RojanServerDbContext"/>'s own doc comment for why - no
/// separate mutable Entity/Mapper pair).
///
/// Deliberately no foreign key to <c>Organizations</c>/<c>Branches</c> -
/// <see cref="Specialist.OrganizationId"/>/<see cref="Specialist.BranchId"/>
/// are plain indexed columns, not FKs, same vertical-slice-independence
/// convention <c>CustomerConfiguration</c>'s own doc comment already
/// establishes. Tenant/branch consistency is validated at the Application
/// layer instead (<c>Application.Specialists.SpecialistService</c>),
/// which is allowed to coordinate across modules.
/// </summary>
public sealed class SpecialistConfiguration : IEntityTypeConfiguration<Specialist>
{
    public void Configure(EntityTypeBuilder<Specialist> builder)
    {
        builder.ToTable("Specialists");

        builder.HasKey(specialist => specialist.Id);

        builder.Property(specialist => specialist.Id)
            .HasMaxLength(64);

        builder.Property(specialist => specialist.OrganizationId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(specialist => specialist.BranchId)
            .HasMaxLength(64);

        builder.Property(specialist => specialist.FullName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(specialist => specialist.Phone)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(specialist => specialist.Email)
            .HasMaxLength(320);

        builder.Property(specialist => specialist.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(specialist => specialist.CreatedAt)
            .IsRequired();

        builder.Property(specialist => specialist.UpdatedAt)
            .IsRequired();

        // Sprint 8 Commit 5: tenant isolation "at the repository level"
        // (see ISpecialistRepository's own doc comment) is enforced by
        // every query filtering on OrganizationId - this index is what
        // keeps that filter cheap as the table grows, not what enforces
        // isolation itself (EfSpecialistRepository's WHERE clauses do
        // that).
        builder.HasIndex(specialist => specialist.OrganizationId);

        builder.HasIndex(specialist => specialist.BranchId);

        builder.HasIndex(specialist => specialist.Phone);
    }
}
