namespace Rojan.Server.Application.Specialists;

/// <summary>Sprint 8 Commit 5: Tenant-Aware Specialist API. No <c>OrganizationId</c> field - same "do not restate already-known context" reasoning <c>Application.Customers.CustomerDto</c> already establishes.</summary>
public sealed record SpecialistDto(
    string Id,
    string? BranchId,
    string FullName,
    string Phone,
    string? Email,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
