namespace Rojan.Server.Application.Customers;

/// <summary>Sprint 8 Commit 4: Tenant-Aware Customer API. No <c>OrganizationId</c> field - the caller already knows its own tenant from the authenticated session (<c>GET api/v1/tenant/current</c>), so restating it on every customer would be redundant, the same "do not restate already-known context" reasoning <c>Application.Authentication.AuthenticationResult</c> follows for tenant fields it does need to restate (there, the caller does not yet know them).</summary>
public sealed record CustomerDto(
    string Id,
    string? BranchId,
    string Name,
    string Phone,
    string? Email,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
