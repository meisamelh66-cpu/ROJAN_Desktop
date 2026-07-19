using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Services;

/// <summary>Default <see cref="IServiceCommandService"/> implementation.</summary>
public sealed class ServiceCommandService : IServiceCommandService
{
    private readonly DomainServices.IServiceRepository _repository;

    public ServiceCommandService(DomainServices.IServiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default)
    {
        var assignment = new DomainServices.SpecialistService(Guid.NewGuid().ToString(), serviceId, Guid.NewGuid().ToString(), specialistName);
        var added = await _repository.AssignSpecialistAsync(assignment, cancellationToken).ConfigureAwait(true);
        return ServiceMapper.MapAssignment(added);
    }

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default) =>
        _repository.UnassignSpecialistAsync(serviceId, assignmentId, cancellationToken);
}
