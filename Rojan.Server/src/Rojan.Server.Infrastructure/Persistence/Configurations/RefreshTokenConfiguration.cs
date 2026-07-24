using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Configurations;

/// <summary>Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. EF Core Fluent API mapping for <see cref="RefreshToken"/>. <see cref="RefreshToken.TokenHash"/> has a unique index - the only lookup path this table ever needs (see <c>Domain.Authentication.IRefreshTokenRepository.GetByTokenHashAsync</c>'s own doc comment). <see cref="RefreshToken.UserId"/> cascades on delete - a token cannot outlive its user.</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .HasMaxLength(64);

        builder.Property(refreshToken => refreshToken.UserId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(refreshToken => refreshToken.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(refreshToken => refreshToken.IssuedAt)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.ExpiresAt)
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique();

        builder.HasIndex(refreshToken => refreshToken.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
