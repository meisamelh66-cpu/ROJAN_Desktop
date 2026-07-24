using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Persistence.Repositories;

/// <summary>Default <see cref="IUserRepository"/> - same "inject the scoped DbContext directly" reasoning as <see cref="EfOrganizationRepository"/>'s own doc comment.</summary>
public sealed class EfUserRepository : IUserRepository
{
    private readonly RojanServerDbContext _dbContext;

    public EfUserRepository(RojanServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user;
    }

    public Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
}
