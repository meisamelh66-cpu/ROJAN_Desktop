using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rojan.Server.Application.Tenancy;

namespace Rojan.Server.Infrastructure.Security;

/// <summary>
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Default
/// <see cref="ITenantContext"/> - reads the same claims
/// <see cref="JwtTokenService.GenerateAccessToken"/> writes
/// (<see cref="JwtRegisteredClaimNames.Sub"/>, <c>org_id</c>,
/// <c>branch_id</c>) off the current authenticated request's
/// <see cref="ClaimsPrincipal"/> via <see cref="IHttpContextAccessor"/>.
/// This is the one place in the entire solution that couples tenant
/// resolution to HTTP - <see cref="ITenantContext"/> itself
/// (<c>Application.Tenancy</c>) stays a plain three-string contract, so
/// Domain/Application never reference <see cref="HttpContext"/> at all.
/// Registered scoped (see
/// <c>DependencyInjection.ServiceCollectionExtensions.AddInfrastructure</c>),
/// matching <see cref="IHttpContextAccessor"/>'s own per-request lifetime -
/// resolving it outside an HTTP request (e.g. from a background job) would
/// throw, which is the correct behavior for something that is, by
/// definition, only meaningful during one.
/// </summary>
public sealed class ClaimsTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string OrganizationId => RequireClaim("org_id");

    public string? BranchId => HttpContext.User.FindFirst("branch_id")?.Value;

    public string UserId => RequireClaim(JwtRegisteredClaimNames.Sub);

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Tenant context was requested outside an HTTP request.");

    private string RequireClaim(string claimType) =>
        HttpContext.User.FindFirst(claimType)?.Value
            ?? throw new InvalidOperationException($"The current request has no '{claimType}' claim - is it authenticated?");
}
