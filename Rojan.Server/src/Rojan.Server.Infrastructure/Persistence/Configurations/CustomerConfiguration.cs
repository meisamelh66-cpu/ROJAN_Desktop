using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rojan.Server.Domain.Customers;

namespace Rojan.Server.Infrastructure.Persistence.Configurations;

/// <summary>
/// Sprint 8 Commit 4: Tenant-Aware Customer API. EF Core Fluent API
/// mapping for <see cref="Customer"/> - Domain record mapped directly via
/// constructor binding, same pattern
/// <c>Configurations.OrganizationConfiguration</c>/<c>BranchConfiguration</c>/
/// <c>UserConfiguration</c> already establish for this backend (see
/// <see cref="RojanServerDbContext"/>'s own doc comment for why - no
/// separate mutable Entity/Mapper pair).
///
/// Deliberately no foreign key to <c>Organizations</c>/<c>Branches</c> -
/// <see cref="Customer.OrganizationId"/>/<see cref="Customer.BranchId"/>
/// are plain indexed columns, not FKs, matching the desktop solution's
/// own vertical-slice-independence convention for cross-module
/// references (see e.g. its <c>BookingEntity</c>, which has no FK to
/// Customer/Specialist either) - <c>Rojan.Server.Domain.Customers</c>
/// must not depend on <c>Domain.Authentication</c>'s schema shape.
/// Tenant/branch consistency is validated at the Application layer
/// instead (<c>Application.Customers.CustomerService</c>), which is
/// allowed to coordinate across modules.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id)
            .HasMaxLength(64);

        builder.Property(customer => customer.OrganizationId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(customer => customer.BranchId)
            .HasMaxLength(64);

        builder.Property(customer => customer.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(customer => customer.Phone)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(customer => customer.Email)
            .HasMaxLength(320);

        builder.Property(customer => customer.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(customer => customer.CreatedAt)
            .IsRequired();

        builder.Property(customer => customer.UpdatedAt)
            .IsRequired();

        // Sprint 8 Commit 4: tenant isolation "at the repository level"
        // (see ICustomerRepository's own doc comment) is enforced by every
        // query filtering on OrganizationId - this index is what keeps
        // that filter cheap as the table grows, not what enforces
        // isolation itself (EfCustomerRepository's WHERE clauses do that).
        builder.HasIndex(customer => customer.OrganizationId);

        builder.HasIndex(customer => customer.BranchId);

        builder.HasIndex(customer => customer.Phone);
    }
}
