using System.ComponentModel.DataAnnotations;

namespace Rojan.Server.Application.Customers;

/// <summary>
/// Sprint 8 Commit 4: Tenant-Aware Customer API. A full-field replacement
/// (name/phone/email/branch/status all in one call), the same shape the
/// desktop solution's own <c>Application.Customers.UpdateCustomerRequest</c>
/// already establishes for its Customer module -
/// <c>Server.CustomerService.UpdateCustomerAsync</c> enforces
/// <c>Domain.Customers.CustomerRules.IsValidTransition</c> only when
/// <see cref="Status"/> actually changes, matching that same precedent
/// exactly. No <c>Id</c>/<c>OrganizationId</c> field - the id comes from
/// the route (<c>PUT api/v1/customers/{id}</c>), the tenant from
/// <c>Application.Tenancy.ITenantContext</c>.
/// </summary>
public sealed record UpdateCustomerRequest(
    [Required, MinLength(1)] string Name,
    [Required, MinLength(1)] string Phone,
    [EmailAddress] string? Email,
    string? BranchId,
    [Required] string Status);
