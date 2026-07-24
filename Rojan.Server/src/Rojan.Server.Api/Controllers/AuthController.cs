using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rojan.Server.Application.Authentication;

namespace Rojan.Server.Api.Controllers;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. The three
/// endpoints this commit implements - <c>POST api/v1/auth/register</c>
/// (register a brand-new organization's owner),
/// <c>POST api/v1/auth/login</c>, and <c>POST api/v1/auth/refresh</c>.
/// All three are <see cref="AllowAnonymousAttribute"/> - a client cannot
/// present a bearer token before it has one. Every action binds the
/// request straight to an <c>Application.Authentication</c> DTO and
/// returns the resulting <see cref="AuthenticationResult"/> unchanged -
/// no separate API-layer contract type, the same "reuse the
/// Application-layer DTO as the wire shape" choice
/// <c>RegisterOrganizationOwnerRequest</c>'s own doc comment explains.
/// <see cref="AuthenticationException"/> subtypes are translated to the
/// appropriate HTTP status here, since that mapping is an API concern,
/// not something <c>AuthenticationService</c> itself should know about.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResult>> Register(RegisterOrganizationOwnerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _authenticationService.RegisterOrganizationOwnerAsync(request, cancellationToken).ConfigureAwait(true));
        }
        catch (EmailAlreadyRegisteredException exception)
        {
            return Conflict(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _authenticationService.LoginAsync(request, cancellationToken).ConfigureAwait(true));
        }
        catch (InvalidCredentialsException exception)
        {
            return Unauthorized(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status401Unauthorized });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthenticationResult>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _authenticationService.RefreshAsync(request, cancellationToken).ConfigureAwait(true));
        }
        catch (InvalidRefreshTokenException exception)
        {
            return Unauthorized(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status401Unauthorized });
        }
    }
}
