namespace Rojan.Server.Domain.Authentication;

/// <summary>Persistence contract for <see cref="RefreshToken"/>. <see cref="GetByTokenHashAsync"/>, never a raw-token lookup - see <see cref="RefreshToken.TokenHash"/>'s own doc comment for why only the hash is ever persisted or queried.</summary>
public interface IRefreshTokenRepository
{
    public Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Persists a revocation (see <see cref="RefreshToken.RevokedAt"/>) - part of the rotate-on-use flow, not a general update operation.</summary>
    public Task RevokeAsync(string refreshTokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);
}
