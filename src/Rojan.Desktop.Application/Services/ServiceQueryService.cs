using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Default <see cref="IServiceQueryService"/> implementation - fetches
/// from <see cref="DomainServices.IServiceRepository"/> (Application is
/// allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="ServiceMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
/// </summary>
public sealed class ServiceQueryService : IServiceQueryService
{
    private readonly DomainServices.IServiceRepository _repository;

    public ServiceQueryService(DomainServices.IServiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = await _repository.GetServicesAsync(cancellationToken).ConfigureAwait(true);
        return services.Select(ServiceMapper.MapService).ToList();
    }

    /// <summary>
    /// Composes over <see cref="DomainServices.IServiceRepository.GetServicesAsync"/>
    /// rather than a dedicated repository search method - same reasoning as
    /// <c>Customers.CustomerQueryService.SearchCustomersAsync</c>.
    /// </summary>
    public async Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var services = await _repository.GetServicesAsync(cancellationToken).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return services.Select(ServiceMapper.MapService).ToList();
        }

        return services
            .Where(service =>
                service.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                service.Category.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                service.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(ServiceMapper.MapService)
            .ToList();
    }
}
