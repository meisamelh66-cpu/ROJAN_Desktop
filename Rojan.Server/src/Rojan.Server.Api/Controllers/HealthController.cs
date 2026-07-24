using Microsoft.AspNetCore.Mvc;

namespace Rojan.Server.Api.Controllers;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. The one controller this commit
/// implements (see the solution's own README - "No controllers except
/// Health"). Deliberately anonymous (no <c>[Authorize]</c>) and free of
/// any business/Infrastructure dependency - a liveness probe must answer
/// even if the database or anything else downstream is unavailable, so it
/// must never depend on them.
/// </summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Returns <c>{"status":"ok"}</c> - see the solution's own README for why the shape is fixed rather than using ASP.NET Core's built-in health check middleware (which defaults to a plain-text body).</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new HealthResponse("ok"));

    private sealed record HealthResponse(string Status);
}
