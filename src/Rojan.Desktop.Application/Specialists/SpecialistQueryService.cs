using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Default <see cref="ISpecialistQueryService"/> implementation - fetches
/// from <see cref="DomainSpecialists.ISpecialistRepository"/> (Application
/// is allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="SpecialistMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
/// </summary>
public sealed class SpecialistQueryService : ISpecialistQueryService
{
    private readonly DomainSpecialists.ISpecialistRepository _repository;

    public SpecialistQueryService(DomainSpecialists.ISpecialistRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default)
    {
        var specialists = await _repository.GetSpecialistsAsync(cancellationToken).ConfigureAwait(true);
        return specialists.Select(SpecialistMapper.MapSpecialist).ToList();
    }

    /// <summary>
    /// Composes over <see cref="DomainSpecialists.ISpecialistRepository.GetSpecialistsAsync"/>
    /// rather than a dedicated repository search method - same reasoning as
    /// <c>Customers.CustomerQueryService.SearchCustomersAsync</c>.
    /// </summary>
    public async Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var specialists = await _repository.GetSpecialistsAsync(cancellationToken).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return specialists.Select(SpecialistMapper.MapSpecialist).ToList();
        }

        return specialists
            .Where(specialist =>
                specialist.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                specialist.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                specialist.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(SpecialistMapper.MapSpecialist)
            .ToList();
    }

    /// <summary>
    /// Composes over <see cref="DomainSpecialists.ISpecialistRepository.GetSpecialistsAsync"/>
    /// (plus <see cref="DomainSpecialists.ISpecialistRepository.GetSkillsAsync"/>
    /// only when <see cref="SpecialistSearchFilter.Skill"/> is set), same
    /// reasoning as <see cref="SearchSpecialistsAsync(string, CancellationToken)"/>
    /// above and <c>Customers.CustomerQueryService.SearchCustomersAsync(CustomerSearchFilter, CancellationToken)</c>.
    /// Every criterion is applied only when present in <paramref name="filter"/>
    /// and ANDed with the rest, so an all-default filter falls through this
    /// method unchanged, ending up identical to <see cref="GetSpecialistsAsync"/>.
    /// <see cref="SpecialistSearchFilter.SearchText"/> matches
    /// <see cref="DomainSpecialists.Specialist.FullName"/>/<see cref="DomainSpecialists.Specialist.Bio"/>/<see cref="DomainSpecialists.Specialist.Phone"/> -
    /// deliberately not Title/Email (matched by the legacy
    /// <see cref="SearchSpecialistsAsync(string, CancellationToken)"/>
    /// overload above), per this commit's specified field set.
    /// </summary>
    public async Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(SpecialistSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var specialists = (await _repository.GetSpecialistsAsync(cancellationToken).ConfigureAwait(true)).ToList();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            specialists = specialists.Where(specialist =>
                specialist.FullName.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                specialist.Bio.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                specialist.Phone.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (filter.Status.HasValue)
        {
            var domainStatus = SpecialistMapper.MapStatusToDomain(filter.Status.Value);
            specialists = specialists.Where(specialist => specialist.Status == domainStatus).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.Skill))
        {
            var matching = new List<DomainSpecialists.Specialist>();
            foreach (var specialist in specialists)
            {
                var skills = await _repository.GetSkillsAsync(specialist.Id, cancellationToken).ConfigureAwait(true);
                if (skills.Any(skill => skill.Name.Contains(filter.Skill, StringComparison.OrdinalIgnoreCase)))
                {
                    matching.Add(specialist);
                }
            }

            specialists = matching;
        }

        return specialists.Select(SpecialistMapper.MapSpecialist).ToList();
    }
}
