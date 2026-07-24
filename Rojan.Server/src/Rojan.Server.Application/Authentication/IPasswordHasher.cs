namespace Rojan.Server.Application.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. The one seam
/// standing between a plain-text password and
/// <c>Domain.Authentication.User.PasswordHash</c> - <c>AuthenticationService</c>
/// never sees or handles a raw password beyond passing it straight
/// through this interface, and never persists anything but the result of
/// <see cref="Hash"/>. Concrete implementation
/// (<c>Infrastructure.Security.Pbkdf2PasswordHasher</c>) is PBKDF2 - see
/// its own doc comment for why.
/// </summary>
public interface IPasswordHasher
{
    public string Hash(string password);

    public bool Verify(string password, string hash);
}
