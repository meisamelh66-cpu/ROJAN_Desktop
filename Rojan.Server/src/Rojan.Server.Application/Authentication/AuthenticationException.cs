namespace Rojan.Server.Application.Authentication;

/// <summary>Base type every <c>IAuthenticationService</c> failure that is a legitimate business outcome (not a bug) is thrown as - lets <c>Api.Controllers.AuthController</c> catch this one type and still branch on the concrete subtype for the right HTTP status.</summary>
public abstract class AuthenticationException : Exception
{
    protected AuthenticationException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown by <c>IAuthenticationService.LoginAsync</c> when the email is
/// not registered, and separately when the password does not match -
/// both cases throw this exact same exception with the exact same
/// message, deliberately. Revealing "no such account" vs "wrong password"
/// tells an attacker which emails are registered; a login endpoint must
/// never leak that distinction.
/// </summary>
public sealed class InvalidCredentialsException() : AuthenticationException("The email or password is incorrect.");

/// <summary>Thrown by <c>IAuthenticationService.RefreshAsync</c> when the supplied refresh token does not match any stored (hashed) token, or the matching one is expired/already revoked (see <c>Domain.Authentication.RefreshToken.IsActive</c>).</summary>
public sealed class InvalidRefreshTokenException() : AuthenticationException("The refresh token is invalid or has expired.");

/// <summary>Thrown by <c>IAuthenticationService.RegisterOrganizationOwnerAsync</c> when <see cref="RegisterOrganizationOwnerRequest.Email"/> already belongs to an existing user (email is globally unique - see <c>Domain.Authentication.IUserRepository.GetByEmailAsync</c>'s own doc comment).</summary>
public sealed class EmailAlreadyRegisteredException() : AuthenticationException("An account with this email already exists.");
