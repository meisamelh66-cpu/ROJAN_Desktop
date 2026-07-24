using System.ComponentModel.DataAnnotations;

namespace Rojan.Server.Application.Customers;

/// <summary>Sprint 8 Commit 4: Tenant-Aware Customer API. Deliberately has no <c>OrganizationId</c> field - the tenant always comes from <c>Application.Tenancy.ITenantContext</c> (the authenticated session), never from request-body/client-supplied data (see <c>CustomerService.CreateCustomerAsync</c>'s own doc comment). <see cref="BranchId"/> is optional and, if present, validated against the caller's own organization by <c>CustomerService</c>, not here.</summary>
public sealed record CreateCustomerRequest(
    [Required, MinLength(1)] string Name,
    [Required, MinLength(1)] string Phone,
    [EmailAddress] string? Email,
    string? BranchId);
