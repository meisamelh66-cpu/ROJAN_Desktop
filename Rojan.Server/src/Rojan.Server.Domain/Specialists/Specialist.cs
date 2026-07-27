namespace Rojan.Server.Domain.Specialists;

/// <summary>
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. Second business module
/// on this backend - deliberately in its own
/// <c>Rojan.Server.Domain.Specialists</c> namespace, not
/// <c>Domain.Authentication</c> or <c>Domain.Customers</c>, matching the
/// same vertical-slice independence <see cref="Rojan.Server.Domain.Customers.Customer"/>
/// already establishes (see its own doc comment).
///
/// <see cref="OrganizationId"/> is required - every specialist belongs to
/// exactly one tenant, enforced by <see cref="ISpecialistRepository"/>'s
/// own contract, same as <c>Domain.Customers.ICustomerRepository</c>.
/// <see cref="BranchId"/> is optional, the same shape
/// <c>Domain.Customers.Customer.BranchId</c> already establishes -
/// consistency between the two (a specialist's branch must belong to the
/// specialist's own organization) is validated by
/// <c>Application.Specialists.SpecialistService</c>, which is allowed to
/// coordinate across modules; Domain itself is not (see this record's own
/// namespace note above).
/// </summary>
public sealed record Specialist(
    string Id,
    string OrganizationId,
    string? BranchId,
    string FullName,
    string Phone,
    string? Email,
    SpecialistStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public bool IsActive => Status == SpecialistStatus.Active;
}
