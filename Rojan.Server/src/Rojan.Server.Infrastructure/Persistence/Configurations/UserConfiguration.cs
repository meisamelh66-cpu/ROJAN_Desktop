using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Configurations;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. EF Core
/// Fluent API mapping for <see cref="User"/>. <see cref="User.Email"/>
/// has a unique index - it is globally unique across every tenant, not
/// just within one organization (see
/// <c>Domain.Authentication.IUserRepository.GetByEmailAsync</c>'s own
/// doc comment: login/registration key off email alone, with no separate
/// "which organization" input). <see cref="User.OrganizationId"/> is
/// required with cascade delete (a user cannot exist without its
/// organization); <see cref="User.BranchId"/> is optional with
/// <see cref="DeleteBehavior.SetNull"/> - deleting a branch must unassign
/// its users, never delete them.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasMaxLength(64);

        builder.Property(user => user.OrganizationId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(user => user.BranchId)
            .HasMaxLength(64);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.Role)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasIndex(user => user.OrganizationId);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(user => user.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(user => user.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
