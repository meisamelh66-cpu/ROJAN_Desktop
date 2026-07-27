using System.ComponentModel.DataAnnotations;

namespace Rojan.Server.Application.Specialists;

/// <summary>Sprint 8 Commit 5: Tenant-Aware Specialist API. Deliberately has no <c>OrganizationId</c> field - the tenant always comes from <c>Application.Tenancy.ITenantContext</c>, never from request-body/client-supplied data (see <c>SpecialistService.CreateSpecialistAsync</c>'s own doc comment). <see cref="BranchId"/> is optional and, if present, validated against the caller's own organization by <c>SpecialistService</c>, not here.</summary>
public sealed record CreateSpecialistRequest(
    [Required, MinLength(1)] string FullName,
    [Required, MinLength(1)] string Phone,
    [EmailAddress] string? Email,
    string? BranchId);
