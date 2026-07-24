namespace Rojan.Server.Application.Tenancy;

/// <summary>Sprint 8 Commit 3: Multi-Tenant Organization Foundation. The response shape for <c>GET api/v1/tenant/current</c> - exactly the fields that endpoint's own spec names, nothing more (no <c>Domain.Authentication.OrganizationStatus</c>/<c>BranchStatus</c> exposed here; those stay an internal <see cref="ITenantService"/> access-control concern, not part of this response).</summary>
public sealed record CurrentTenantDto(
    string OrganizationId,
    string OrganizationName,
    string? BranchId,
    string? BranchName,
    string UserId);
