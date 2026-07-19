using Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

/// <summary>Configurable <see cref="IServiceQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubServiceQueryService : IServiceQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ServiceDto>>> _getServices;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<ServiceDto>>>? _searchServices;

    public StubServiceQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ServiceDto>>> getServices,
        Func<string, CancellationToken, Task<IReadOnlyList<ServiceDto>>>? searchServices = null)
    {
        _getServices = getServices;
        _searchServices = searchServices;
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
}
