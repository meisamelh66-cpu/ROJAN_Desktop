using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Configurable <see cref="ISpecialistQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubSpecialistQueryService : ISpecialistQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<SpecialistDto>>> _getSpecialists;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<SpecialistDto>>>? _searchSpecialists;

    public StubSpecialistQueryService(
        Func<CancellationToken, Task<IReadOnlyList<SpecialistDto>>> getSpecialists,
        Func<string, CancellationToken, Task<IReadOnlyList<SpecialistDto>>>? searchSpecialists = null)
    {
        _getSpecialists = getSpecialists;
        _searchSpecialists = searchSpecialists;
    }

    public Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default) =>
        _getSpecialists(cancellationToken);

    public async Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        if (_searchSpecialists is not null)
        {
            return await _searchSpecialists(searchText, cancellationToken).ConfigureAwait(true);
        }

        var specialists = await _getSpecialists(cancellationToken).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return specialists;
        }

        return specialists
            .Where(specialist =>
                specialist.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                specialist.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                specialist.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
