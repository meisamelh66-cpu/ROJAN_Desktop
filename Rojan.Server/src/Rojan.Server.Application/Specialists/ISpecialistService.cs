namespace Rojan.Server.Application.Specialists;

/// <summary>
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. Every operation is
/// scoped to <c>Application.Tenancy.ITenantContext.OrganizationId</c> -
/// none of them accept an organization id as a parameter or read one from
/// a request DTO (see <see cref="CreateSpecialistRequest"/>/
/// <see cref="UpdateSpecialistRequest"/>'s own doc comments); the tenant
/// always comes from the authenticated session, never from
/// caller-supplied data. Same shape as <c>Application.Customers.ICustomerService</c>.
/// </summary>
public interface ISpecialistService
{
    public Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="SpecialistNotFoundException"/> if <paramref name="specialistId"/> does not exist within the caller's own organization.</summary>
    public Task<SpecialistDto> GetSpecialistAsync(string specialistId, CancellationToken cancellationToken = default);

    /// <summary>Every specialist within the caller's own organization - never another tenant's.</summary>
    public Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="SpecialistNotFoundException"/> if <paramref name="specialistId"/> does not exist within the caller's own organization.</summary>
    public Task<SpecialistDto> UpdateSpecialistAsync(string specialistId, UpdateSpecialistRequest request, CancellationToken cancellationToken = default);
}
