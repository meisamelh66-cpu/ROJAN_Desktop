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

    /// <summary>
    /// Composes over <see cref="DomainServices.IServiceRepository.GetServicesAsync"/>
    /// (plus <see cref="DomainServices.IServiceRepository.GetAssignedSpecialistsAsync"/>
    /// only when <see cref="ServiceSearchFilter.IsAssigned"/> is set), same
    /// reasoning as <see cref="SearchServicesAsync(string, CancellationToken)"/>
    /// above and <c>Customers.CustomerQueryService.SearchCustomersAsync(CustomerSearchFilter, CancellationToken)</c>.
    /// Every criterion is applied only when present in <paramref name="filter"/>
    /// and ANDed with the rest, so an all-default filter falls through this
    /// method unchanged, ending up identical to <see cref="GetServicesAsync"/>.
    /// <see cref="ServiceSearchFilter.SearchText"/> deliberately matches only
    /// <see cref="DomainServices.Service.Name"/>/<see cref="DomainServices.Service.Description"/> -
    /// not Category, unlike the legacy <see cref="SearchServicesAsync(string, CancellationToken)"/>
    /// overload above - since <see cref="ServiceSearchFilter.Category"/> is
    /// now its own first-class filter, the same "enums get a dedicated
    /// filter parameter, free text only matches real string fields"
    /// convention <c>BookingSearchFilter</c>/<c>CustomerSearchFilter</c>
    /// already follow.
    /// </summary>
    public async Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(ServiceSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var services = (await _repository.GetServicesAsync(cancellationToken).ConfigureAwait(true)).ToList();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            services = services.Where(service =>
                service.Name.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                service.Description.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (filter.Category.HasValue)
        {
            var domainCategory = ServiceMapper.MapCategoryToDomain(filter.Category.Value);
            services = services.Where(service => service.Category == domainCategory).ToList();
        }

        if (filter.Status.HasValue)
        {
            var domainStatus = ServiceMapper.MapStatusToDomain(filter.Status.Value);
            services = services.Where(service => service.Status == domainStatus).ToList();
        }

        if (filter.MinDurationMinutes.HasValue)
        {
            services = services.Where(service => service.DurationMinutes >= filter.MinDurationMinutes.Value).ToList();
        }

        if (filter.MaxDurationMinutes.HasValue)
        {
            services = services.Where(service => service.DurationMinutes <= filter.MaxDurationMinutes.Value).ToList();
        }

        if (filter.MinPrice.HasValue)
        {
            services = services.Where(service => ServicePriceParser.Parse(service.Price) >= filter.MinPrice.Value).ToList();
        }

        if (filter.MaxPrice.HasValue)
        {
            services = services.Where(service => ServicePriceParser.Parse(service.Price) <= filter.MaxPrice.Value).ToList();
        }

        if (filter.IsAssigned.HasValue)
        {
            var matching = new List<DomainServices.Service>();
            foreach (var service in services)
            {
                var assignments = await _repository.GetAssignedSpecialistsAsync(service.Id, cancellationToken).ConfigureAwait(true);
                if ((assignments.Count > 0) == filter.IsAssigned.Value)
                {
                    matching.Add(service);
                }
            }

            services = matching;
        }

        return services.Select(ServiceMapper.MapService).ToList();
    }

    public async Task<IReadOnlyList<ServiceCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken).ConfigureAwait(true);
        return categories.Select(category => new ServiceCategoryDto(category.Id, category.Name)).ToList();
    }
}
