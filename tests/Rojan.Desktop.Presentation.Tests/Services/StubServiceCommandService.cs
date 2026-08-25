using Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubServiceCommandService : IServiceCommandService
{
    public List<(string ServiceId, string SpecialistName)> AssignCalls { get; } = [];

    public List<(string ServiceId, string AssignmentId)> UnassignCalls { get; } = [];

    public List<CreateServiceRequest> CreateRequests { get; } = [];

    public List<UpdateServiceRequest> UpdateRequests { get; } = [];

    /// <summary>Service Catalog Authoring: when set, <see cref="CreateServiceAsync"/> throws this instead of succeeding - lets a test drive the create-failure path.</summary>
    public Exception? CreateServiceException { get; set; }

    /// <summary>Service Catalog Authoring: when set, <see cref="UpdateServiceAsync"/> throws this instead of succeeding.</summary>
    public Exception? UpdateServiceException { get; set; }

    public Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default)
    {
        AssignCalls.Add((serviceId, specialistName));
        return Task.FromResult(new AssignedSpecialistDto("new-assignment", serviceId, "new-specialist", specialistName));
    }

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default)
    {
        UnassignCalls.Add((serviceId, assignmentId));
        return Task.CompletedTask;
    }

    public Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);

        if (CreateServiceException is not null)
        {
            return Task.FromException<ServiceDto>(CreateServiceException);
        }

        return Task.FromResult(new ServiceDto(
            "new-service", request.Name, ServiceCategory.Other, ServiceStatus.Active,
            request.DurationMinutes, request.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.Description ?? string.Empty, CategoryId: request.CategoryId));
    }

    public Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        UpdateRequests.Add(request);

        if (UpdateServiceException is not null)
        {
            return Task.FromException<ServiceDto>(UpdateServiceException);
        }

        return Task.FromResult(new ServiceDto(
            request.Id, request.Name, ServiceCategory.Other, request.Status,
            request.DurationMinutes, request.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.Description ?? string.Empty, CategoryId: request.CategoryId));
    }
}
