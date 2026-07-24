using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rojan.Server.Application.Tenancy;

namespace Rojan.Server.Api.Controllers;

/// <summary>
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. The one
/// endpoint this commit implements - <c>GET api/v1/tenant/current</c>.
/// Unlike <see cref="AuthController"/>, this one requires authentication
/// (<see cref="AuthorizeAttribute"/>, no <see cref="AllowAnonymousAttribute"/>
/// anywhere) - a caller with no valid bearer token has no tenant to ask
/// about. No business operations (no Customer/Specialist/Service/Booking
/// data) - this is tenant-shell information only.
/// </summary>
[ApiController]
[Route("api/v1/tenant")]
[Authorize]
public sealed class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CurrentTenantDto>> GetCurrent(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _tenantService.GetCurrentTenantAsync(cancellationToken).ConfigureAwait(true));
        }
        catch (TenantAccessDeniedException exception)
        {
            return Unauthorized(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status401Unauthorized });
        }
    }
}
