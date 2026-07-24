namespace Rojan.Server.Application.Tenancy;

/// <summary>
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. Who is making
/// the current call and on behalf of which tenant - the one piece of
/// per-request state every tenant-aware Application service needs
/// (<see cref="Tenancy.ITenantService"/>, and every future business
/// module service). Deliberately just three strings/nullable-string - no
/// <c>HttpContext</c>, no <c>ClaimsPrincipal</c>, nothing web-specific
/// anywhere in this contract, so Domain/Application stay completely
/// unaware of HTTP. The concrete implementation
/// (<c>Infrastructure.Security.ClaimsTenantContext</c>) is the one place
/// that actually reads an authenticated request's claims - see its own
/// doc comment.
/// </summary>
public interface ITenantContext
{
    public string OrganizationId { get; }

    public string? BranchId { get; }

    public string UserId { get; }
}
