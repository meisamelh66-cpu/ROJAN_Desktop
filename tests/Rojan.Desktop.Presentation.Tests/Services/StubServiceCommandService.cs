using System.Globalization;
using Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubServiceCommandService : IServiceCommandService
{
    public List<(string ServiceId, string SpecialistName)> AssignCalls { get; } = [];

    public List<(string ServiceId, string AssignmentId)> UnassignCalls { get; } = [];

    public List<CreateServiceRequest> CreateCalls { get; } = [];

    public List<UpdateServiceRequest> UpdateCalls { get; } = [];

    public List<string> DeactivateCalls { get; } = [];

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
        CreateCalls.Add(request);
        return Task.FromResult(new ServiceDto("service-new", request.Name, ServiceCategory.Other, ServiceStatus.Active, request.DurationMinutes, request.Price.ToString(CultureInfo.InvariantCulture), request.Description));
    }

    public Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        UpdateCalls.Add(request);
        return Task.FromResult(new ServiceDto(request.Id, request.Name, ServiceCategory.Other, ServiceStatus.Active, request.DurationMinutes, request.Price.ToString(CultureInfo.InvariantCulture), request.Description));
    }

    public Task DeactivateServiceAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        DeactivateCalls.Add(serviceId);
        return Task.CompletedTask;
    }
}
