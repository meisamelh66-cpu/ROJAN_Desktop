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
}
