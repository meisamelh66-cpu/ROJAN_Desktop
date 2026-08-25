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

    /// <summary>
    /// Service Catalog Authoring. Field-shape validation
    /// (<see cref="DomainServices.ServiceRules.IsValidName"/>/<see cref="DomainServices.ServiceRules.IsValidDuration"/>/<see cref="DomainServices.ServiceRules.IsValidPrice"/>)
    /// throws before ever calling the repository - same "validate the
    /// shape, then delegate" pattern <c>Bookings.BookingCommandService.CreateBookingAsync</c>'s
    /// own <c>BookingRules.IsValidDuration</c> check already establishes.
    /// </summary>
    public async Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateFields(request.Name, request.DurationMinutes, request.Price);

        var created = await _repository.CreateServiceAsync(
            request.CategoryId, request.Name, request.Description, request.DurationMinutes, request.Price, cancellationToken).ConfigureAwait(true);
        return ServiceMapper.MapService(created);
    }

    /// <summary>
    /// Service Catalog Authoring. Same two-layer validation shape as
    /// <c>Specialists.SpecialistCommandService.UpdateSpecialistAsync</c>:
    /// field-shape validation always runs; <see cref="DomainServices.ServiceRules.IsValidTransition"/>
    /// only runs when the requested status actually differs from the
    /// existing one, so an edit that doesn't touch status is never treated
    /// as an illegal transition.
    /// </summary>
    public async Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateFields(request.Name, request.DurationMinutes, request.Price);

        var existing = await _repository.GetServiceByIdAsync(request.Id, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Service '{request.Id}' was not found.");

        var requestedStatus = ServiceMapper.MapStatusToDomain(request.Status);
        if (requestedStatus != existing.Status && !DomainServices.ServiceRules.IsValidTransition(existing.Status, requestedStatus))
        {
            throw new InvalidOperationException($"Cannot transition service from {existing.Status} to {requestedStatus}.");
        }

        var updated = await _repository.UpdateServiceAsync(
            request.Id, request.CategoryId, request.Name, request.Description, request.DurationMinutes, request.Price, requestedStatus, cancellationToken).ConfigureAwait(true);
        return ServiceMapper.MapService(updated);
    }

    private static void ValidateFields(string name, int durationMinutes, decimal price)
    {
        if (!DomainServices.ServiceRules.IsValidName(name))
        {
            throw new ArgumentException("Service name must not be blank and must be 255 characters or fewer.", nameof(name));
        }

        if (!DomainServices.ServiceRules.IsValidDuration(durationMinutes))
        {
            throw new ArgumentException($"Duration {durationMinutes} minutes is not valid.", nameof(durationMinutes));
        }

        if (!DomainServices.ServiceRules.IsValidPrice(price))
        {
            throw new ArgumentException($"Price {price} is not valid.", nameof(price));
        }
    }
}
