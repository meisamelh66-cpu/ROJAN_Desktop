using Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

/// <summary>
/// Configurable <see cref="IServiceQueryService"/> test double - same
/// reasoning as Customers.StubCustomerQueryService/Bookings.StubBookingQueryService.
/// <see cref="SearchServicesAsync(ServiceSearchFilter, CancellationToken)"/>
/// defaults to ignoring the filter and returning whatever GetServicesAsync
/// would, so tests that only supply <c>getServices</c> keep working
/// unchanged now that <c>ServicePageViewModel</c> calls
/// <c>SearchServicesAsync(ServiceSearchFilter)</c> instead of
/// <c>GetServicesAsync</c> for every load (Sprint 5 Commit 2);
/// <see cref="SearchCalls"/> records every filter it was asked to search
/// with, in call order, so ViewModel tests can assert on the composed
/// <see cref="ServiceSearchFilter"/> without needing a real filtering
/// implementation here.
/// </summary>
internal sealed class StubServiceQueryService : IServiceQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ServiceDto>>> _getServices;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<ServiceDto>>>? _searchServices;
    private readonly Func<ServiceSearchFilter, CancellationToken, Task<IReadOnlyList<ServiceDto>>> _searchServicesByFilter;

    /// <summary>Every filter this stub was asked to search with, in call order.</summary>
    public List<ServiceSearchFilter> SearchCalls { get; } = [];

    /// <summary>Categories <see cref="GetCategoriesAsync"/> returns - empty by default, settable by tests that need a New Service category picker populated.</summary>
    public List<ServiceCategoryDto> Categories { get; } = [];

    public StubServiceQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ServiceDto>>> getServices,
        Func<string, CancellationToken, Task<IReadOnlyList<ServiceDto>>>? searchServices = null,
        Func<ServiceSearchFilter, CancellationToken, Task<IReadOnlyList<ServiceDto>>>? searchServicesByFilter = null)
    {
        _getServices = getServices;
        _searchServices = searchServices;
        _searchServicesByFilter = searchServicesByFilter ?? ((_, cancellationToken) => _getServices(cancellationToken));
    }

    public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        _getServices(cancellationToken);

    public async Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(string searchText, CancellationToken cancellationToken = default)
    {
        if (_searchServices is not null)
        {
            return await _searchServices(searchText, cancellationToken).ConfigureAwait(true);
        }

        var services = await _getServices(cancellationToken).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return services;
        }

        return services
            .Where(service =>
                service.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                service.Category.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                service.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(ServiceSearchFilter filter, CancellationToken cancellationToken = default)
    {
        SearchCalls.Add(filter);
        return _searchServicesByFilter(filter, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceCategoryDto>>(Categories.ToList());
}
