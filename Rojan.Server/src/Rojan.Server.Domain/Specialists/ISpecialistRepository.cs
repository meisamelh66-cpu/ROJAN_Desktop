namespace Rojan.Server.Domain.Specialists;

/// <summary>
/// Persistence contract for <see cref="Specialist"/>. <see cref="GetByIdAsync"/>
/// takes <c>organizationId</c> as a required parameter, not just
/// <c>specialistId</c> - tenant isolation is enforced here, at the
/// repository/query level, not left to
/// <c>Application.Specialists.SpecialistService</c> to remember to filter
/// results after the fact. A lookup for a specialist that exists but
/// belongs to a different organization returns <see langword="null"/>,
/// exactly the same as a lookup for a specialist that does not exist at
/// all - the caller cannot distinguish "wrong tenant" from "never
/// existed," which is the point (an API response must never confirm
/// another tenant's data exists), same as <c>Domain.Customers.ICustomerRepository</c>.
/// </summary>
public interface ISpecialistRepository
{
    public Task<Specialist> CreateAsync(Specialist specialist, CancellationToken cancellationToken = default);

    public Task<Specialist?> GetByIdAsync(string organizationId, string specialistId, CancellationToken cancellationToken = default);

    /// <summary>Every specialist within one tenant, never across organizations.</summary>
    public Task<IReadOnlyList<Specialist>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an update to an already-loaded <see cref="Specialist"/>.
    /// Does not itself re-check <see cref="Specialist.OrganizationId"/> -
    /// callers must only ever pass a record obtained from
    /// <see cref="GetByIdAsync"/> (already tenant-scoped) with its
    /// <see cref="Specialist.OrganizationId"/> preserved unchanged, never
    /// one built from caller-supplied/request-body data (see
    /// <c>Application.Specialists.SpecialistService.UpdateSpecialistAsync</c>'s
    /// own doc comment).
    /// </summary>
    public Task<Specialist> UpdateAsync(Specialist specialist, CancellationToken cancellationToken = default);
}
