using Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Minimal <see cref="IServiceQueryService"/> test double - only <see cref="GetServicesAsync"/> is exercised by <see cref="BookingWorkflowServiceTests"/>, so <see cref="SearchServicesAsync(string, CancellationToken)"/> just delegates to it.</summary>
internal sealed class StubServiceQueryService : IServiceQueryService
{
    private readonly IReadOnlyList<ServiceDto> _services;

    public StubServiceQueryService(IReadOnlyList<ServiceDto> services)
    {
        _services = services;
    }

    public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_services);

    public Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(_services);

    public Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(ServiceSearchFilter filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(_services);

    public Task<IReadOnlyList<ServiceCategoryOptionDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceCategoryOptionDto>>([]);
}
