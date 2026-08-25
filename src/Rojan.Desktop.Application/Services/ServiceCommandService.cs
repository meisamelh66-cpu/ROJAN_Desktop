using System.Globalization;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Services;

/// <summary>Default <see cref="IServiceCommandService"/> implementation. Service Catalog Management: <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/>/<see cref="DeactivateServiceAsync"/> build/replace a <see cref="DomainServices.Service"/> and hand it to <see cref="_repository"/> - Backend confirms every field value; nothing here validates or decides on its own. <see cref="decimal"/> prices are stringified with <see cref="CultureInfo.InvariantCulture"/> (never a currency-symbol-formatted display string) purely to satisfy <see cref="DomainServices.Service.Price"/>'s string shape for the trip through the repository - <c>BackendServiceRepository</c> parses that exact same invariant format back on the wire, not the free-text <c>ServicePriceParser</c> used for filter comparisons.</summary>
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

    public async Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = new DomainServices.Service(
            Id: string.Empty,
            request.Name,
            DomainServices.ServiceCategory.Other,
            DomainServices.ServiceStatus.Active,
            request.DurationMinutes,
            request.Price.ToString(CultureInfo.InvariantCulture),
            request.Description,
            CategoryName: null,
            CategoryId: request.CategoryId);

        var created = await _repository.CreateServiceAsync(service, cancellationToken).ConfigureAwait(true);
        return ServiceMapper.MapService(created);
    }

    public async Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetServiceByIdAsync(request.Id, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Service '{request.Id}' was not found.");

        var service = existing with
        {
            Name = request.Name,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price.ToString(CultureInfo.InvariantCulture),
            Description = request.Description,
        };

        var updated = await _repository.UpdateServiceAsync(service, cancellationToken).ConfigureAwait(true);
        return ServiceMapper.MapService(updated);
    }

    public async Task DeactivateServiceAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetServiceByIdAsync(serviceId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Service '{serviceId}' was not found.");

        await _repository.DeactivateServiceAsync(existing.CategoryId, serviceId, cancellationToken).ConfigureAwait(true);
    }
}
