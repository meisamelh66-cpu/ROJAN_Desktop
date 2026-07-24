using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Tests.Authentication;

/// <summary>In-memory <see cref="IUserRepository"/> test double - same reasoning the desktop solution's own <c>StubCustomerRepository</c> already establishes: exposes its backing list directly so tests can both seed state and assert on what a call wrote.</summary>
internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        Users.Add(user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(user => user.Email == email));
}

internal sealed class FakeOrganizationRepository : IOrganizationRepository
{
    public List<Organization> Organizations { get; } = [];

    public Task<Organization> CreateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        Organizations.Add(organization);
        return Task.FromResult(organization);
    }

    public Task<Organization?> GetByIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Organizations.FirstOrDefault(organization => organization.Id == organizationId));
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Tokens.Add(refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public Task RevokeAsync(string refreshTokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        var index = Tokens.FindIndex(token => token.Id == refreshTokenId);
        if (index >= 0)
        {
            Tokens[index] = Tokens[index] with { RevokedAt = revokedAt };
        }

        return Task.CompletedTask;
    }
}
