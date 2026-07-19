using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Default <see cref="IServiceProfileQueryService"/> implementation -
/// fetches the service plus its assigned specialists from
/// <see cref="DomainServices.IServiceRepository"/> and assembles the
/// aggregate <see cref="ServiceProfileDto"/>.
/// </summary>
public sealed class ServiceProfileQueryService : IServiceProfileQueryService
{
    private readonly DomainServices.IServiceRepository _repository;

    public ServiceProfileQueryService(DomainServices.IServiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceProfileDto> GetProfileAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetServiceByIdAsync(serviceId, cancellationToken).ConfigureAwait(true);
        if (service is null)
        {
            throw new InvalidOperationException($"Service '{serviceId}' was not found.");
        }

        var assignments = await _repository.GetAssignedSpecialistsAsync(serviceId, cancellationToken).ConfigureAwait(true);

        return new ServiceProfileDto(
            ServiceMapper.MapService(service),
            assignments.Select(ServiceMapper.MapAssignment).ToList());
    }
}
