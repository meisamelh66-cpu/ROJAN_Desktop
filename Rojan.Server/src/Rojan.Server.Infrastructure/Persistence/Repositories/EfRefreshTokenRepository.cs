using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Repositories;

/// <summary>
/// Default <see cref="IRefreshTokenRepository"/> - same "inject the
/// scoped DbContext directly" reasoning as
/// <see cref="EfOrganizationRepository"/>'s own doc comment.
/// <see cref="RevokeAsync"/> loads the tracked record and copies the
/// revoked version's values onto it via <c>ChangeTracking.CurrentValues.SetValues</c>
/// rather than replacing the entity outright - the standard EF Core
/// pattern for updating an immutable record already being change-tracked.
/// </summary>
public sealed class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly RojanServerDbContext _dbContext;

    public EfRefreshTokenRepository(RojanServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return refreshToken;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens.FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeAsync(string refreshTokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(refreshToken => refreshToken.Id == refreshTokenId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(existing with { RevokedAt = revokedAt });
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
