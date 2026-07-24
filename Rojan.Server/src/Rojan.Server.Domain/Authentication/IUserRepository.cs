namespace Rojan.Server.Domain.Authentication;

/// <summary>Persistence contract for <see cref="User"/>. <see cref="GetByEmailAsync"/> exists because login/registration both key off email (globally unique - see <c>Infrastructure.Persistence.Configurations.UserConfiguration</c>'s own doc comment), not <see cref="User.Id"/>.</summary>
public interface IUserRepository
{
    public Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    public Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
