using Rojan.Server.Application.Tenancy;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Domain.Specialists;

namespace Rojan.Server.Application.Specialists;

/// <summary>
/// Default <see cref="ISpecialistService"/>. Every method reads
/// <see cref="ITenantContext.OrganizationId"/> itself rather than
/// accepting it as a parameter - the one and only source of tenant
/// identity, never request-body/client-supplied data (see this class's
/// own interface doc comment). <see cref="UpdateSpecialistAsync"/> always
/// loads the existing record first (via
/// <see cref="ISpecialistRepository.GetByIdAsync"/>, itself tenant-scoped)
/// and preserves its <see cref="Specialist.OrganizationId"/> unchanged
/// when building the updated record - same "editing must never silently
/// move a record to a different tenant/branch" reasoning
/// <c>Application.Customers.CustomerService.UpdateCustomerAsync</c>
/// already establishes.
/// </summary>
public sealed class SpecialistService : ISpecialistService
{
    private readonly ITenantContext _tenantContext;
    private readonly ISpecialistRepository _specialistRepository;
    private readonly IBranchRepository _branchRepository;

    public SpecialistService(ITenantContext tenantContext, ISpecialistRepository specialistRepository, IBranchRepository branchRepository)
    {
        _tenantContext = tenantContext;
        _specialistRepository = specialistRepository;
        _branchRepository = branchRepository;
    }

    public async Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureValidBranchAsync(request.BranchId, cancellationToken).ConfigureAwait(true);

        var now = DateTimeOffset.UtcNow;
        var specialist = new Specialist(
            Guid.NewGuid().ToString(),
            _tenantContext.OrganizationId,
            request.BranchId,
            request.FullName,
            request.Phone,
            request.Email,
            SpecialistStatus.Active,
            now,
            now);

        var created = await _specialistRepository.CreateAsync(specialist, cancellationToken).ConfigureAwait(true);

        return Map(created);
    }

    public async Task<SpecialistDto> GetSpecialistAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var specialist = await _specialistRepository.GetByIdAsync(_tenantContext.OrganizationId, specialistId, cancellationToken).ConfigureAwait(true)
            ?? throw new SpecialistNotFoundException(specialistId);

        return Map(specialist);
    }

    public async Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default)
    {
        var specialists = await _specialistRepository.GetByOrganizationIdAsync(_tenantContext.OrganizationId, cancellationToken).ConfigureAwait(true);

        return specialists.Select(Map).ToList();
    }

    public async Task<SpecialistDto> UpdateSpecialistAsync(string specialistId, UpdateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _specialistRepository.GetByIdAsync(_tenantContext.OrganizationId, specialistId, cancellationToken).ConfigureAwait(true)
            ?? throw new SpecialistNotFoundException(specialistId);

        if (!Enum.TryParse<SpecialistStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            throw new InvalidSpecialistStatusException($"'{request.Status}' is not a recognized specialist status.");
        }

        if (newStatus != existing.Status && !SpecialistRules.IsValidTransition(existing.Status, newStatus))
        {
            throw new InvalidSpecialistStatusException($"Cannot transition specialist from {existing.Status} to {newStatus}.");
        }

        await EnsureValidBranchAsync(request.BranchId, cancellationToken).ConfigureAwait(true);

        var updated = existing with
        {
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            BranchId = request.BranchId,
            Status = newStatus,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var saved = await _specialistRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(true);

        return Map(saved);
    }

    private async Task EnsureValidBranchAsync(string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is null)
        {
            return;
        }

        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(true);
        if (branch is null || !UserRules.IsValidBranchAssignment(_tenantContext.OrganizationId, branch))
        {
            throw new InvalidSpecialistBranchException(branchId);
        }
    }

    private static SpecialistDto Map(Specialist specialist) =>
        new(specialist.Id, specialist.BranchId, specialist.FullName, specialist.Phone, specialist.Email, specialist.Status.ToString(), specialist.CreatedAt, specialist.UpdatedAt);
}
