namespace Rojan.Desktop.Application.Specialists;

/// <summary>Read-only use case Presentation depends on to load Specialists - the only way Presentation ever reaches specialist data, never through Domain/Infrastructure directly.</summary>
public interface ISpecialistQueryService
{
    public Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns specialists whose name, title, or email contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every specialist. Predates the <see cref="SpecialistSearchFilter"/> overload below (Sprint 5 Commit 4 added that one alongside this one rather than replacing it, same reasoning <c>Customers.ICustomerQueryService.SearchCustomersAsync(string, CancellationToken)</c>'s own doc comment documents) - kept as part of the public interface surface for its existing test-double coverage, even though <c>SpecialistPageViewModel</c> now calls the filter-based overload exclusively.</summary>
    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>Returns specialists matching every non-null/non-empty criterion in <paramref name="filter"/> (ANDed) - an all-default <see cref="SpecialistSearchFilter"/> returns every specialist, identical to <see cref="GetSpecialistsAsync"/>.</summary>
    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(SpecialistSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Booking Eligibility Filter: the real, backend-owned service ids this
    /// specialist is eligible to perform - a thin passthrough to
    /// <see cref="Domain.Specialists.ISpecialistRepository.GetAssignedServiceIdsAsync"/>,
    /// the same capability Specialist-Service Assignment already built.
    /// An empty list is not "eligible for nothing" - it is ROJAN_Backend's
    /// own opt-in default, meaning "no restriction, eligible for every
    /// service" (see <c>ai.rojan.backend.domain.salon.isSpecialistEligibleForService</c>'s
    /// own doc comment on the backend side). Callers filtering by
    /// eligibility must branch on emptiness explicitly, never treat an
    /// empty list as "no matches".
    /// </summary>
    public Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default);
}
