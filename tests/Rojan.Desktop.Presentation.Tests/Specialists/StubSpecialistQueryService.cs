using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>
/// Configurable <see cref="ISpecialistQueryService"/> test double - same
/// reasoning as Customers.StubCustomerQueryService/Bookings.StubBookingQueryService/
/// Services.StubServiceQueryService. <see cref="SearchSpecialistsAsync(SpecialistSearchFilter, CancellationToken)"/>
/// defaults to ignoring the filter and returning whatever GetSpecialistsAsync
/// would, so tests that only supply <c>getSpecialists</c> keep working
/// unchanged now that <c>SpecialistPageViewModel</c> calls
/// <c>SearchSpecialistsAsync(SpecialistSearchFilter)</c> instead of
/// <c>GetSpecialistsAsync</c> for every load (Sprint 5 Commit 4);
/// <see cref="SearchCalls"/> records every filter it was asked to search
/// with, in call order, so ViewModel tests can assert on the composed
/// <see cref="SpecialistSearchFilter"/> without needing a real filtering
/// implementation here.
/// </summary>
internal sealed class StubSpecialistQueryService : ISpecialistQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<SpecialistDto>>> _getSpecialists;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<SpecialistDto>>>? _searchSpecialists;
    private readonly Func<SpecialistSearchFilter, CancellationToken, Task<IReadOnlyList<SpecialistDto>>> _searchSpecialistsByFilter;

    /// <summary>Every filter this stub was asked to search with, in call order.</summary>
    public List<SpecialistSearchFilter> SearchCalls { get; } = [];

    public StubSpecialistQueryService(
        Func<CancellationToken, Task<IReadOnlyList<SpecialistDto>>> getSpecialists,
        Func<string, CancellationToken, Task<IReadOnlyList<SpecialistDto>>>? searchSpecialists = null,
        Func<SpecialistSearchFilter, CancellationToken, Task<IReadOnlyList<SpecialistDto>>>? searchSpecialistsByFilter = null)
    {
        _getSpecialists = getSpecialists;
        _searchSpecialists = searchSpecialists;
        _searchSpecialistsByFilter = searchSpecialistsByFilter ?? ((_, cancellationToken) => _getSpecialists(cancellationToken));
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

    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(SpecialistSearchFilter filter, CancellationToken cancellationToken = default)
    {
        SearchCalls.Add(filter);
        return _searchSpecialistsByFilter(filter, cancellationToken);
    }
}
