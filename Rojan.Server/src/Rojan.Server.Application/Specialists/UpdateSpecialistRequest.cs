using System.ComponentModel.DataAnnotations;

namespace Rojan.Server.Application.Specialists;

/// <summary>
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. A full-field
/// replacement (name/phone/email/branch/status all in one call), same
/// shape <c>Application.Customers.UpdateCustomerRequest</c> already
/// establishes - <c>Server.SpecialistService.UpdateSpecialistAsync</c>
/// enforces <c>Domain.Specialists.SpecialistRules.IsValidTransition</c>
/// only when <see cref="Status"/> actually changes, matching that same
/// precedent exactly. No <c>Id</c>/<c>OrganizationId</c> field - the id
/// comes from the route (<c>PUT api/v1/specialists/{id}</c>), the tenant
/// from <c>Application.Tenancy.ITenantContext</c>.
/// </summary>
public sealed record UpdateSpecialistRequest(
    [Required, MinLength(1)] string FullName,
    [Required, MinLength(1)] string Phone,
    [EmailAddress] string? Email,
    string? BranchId,
    [Required] string Status);
