using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Tests.Services;

/// <summary>In-memory, mutable <see cref="IServiceRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubServiceRepository : IServiceRepository
{
    public List<Service> Services { get; } = [];

    public List<SpecialistService> Assignments { get; } = [];

    public List<ServiceCategoryOption> Categories { get; } = [];

    public List<Service> CreateCalls { get; } = [];

    public List<Service> UpdateCalls { get; } = [];

    public List<(string CategoryId, string ServiceId)> DeactivateCalls { get; } = [];

    public bool ThrowOnCreate { get; set; }

    public bool ThrowOnUpdate { get; set; }

    public bool ThrowOnDeactivate { get; set; }

    public StubServiceRepository()
    {
    }

    public StubServiceRepository(IReadOnlyList<Service> services)
    {
        Services.AddRange(services);
    }

    public Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Service>>(Services.ToList());

    public Task<Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Services.FirstOrDefault(service => service.Id == serviceId));

    public Task<IReadOnlyList<SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistService>>(Assignments.Where(assignment => assignment.ServiceId == serviceId).ToList());

    public Task<SpecialistService> AssignSpecialistAsync(SpecialistService assignment, CancellationToken cancellationToken = default)
    {
        Assignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default)
    {
        Assignments.RemoveAll(assignment => assignment.ServiceId == serviceId && assignment.Id == assignmentId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ServiceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceCategoryOption>>(Categories.ToList());

    public Task<Service> CreateServiceAsync(Service service, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCreate)
        {
            throw new InvalidOperationException("Failed to create service.");
        }

        CreateCalls.Add(service);
        var created = service with { Id = "service-new" };
        Services.Add(created);
        return Task.FromResult(created);
    }

    public Task<Service> UpdateServiceAsync(Service service, CancellationToken cancellationToken = default)
    {
        if (ThrowOnUpdate)
        {
            throw new InvalidOperationException("Failed to update service.");
        }

        UpdateCalls.Add(service);
        var index = Services.FindIndex(existing => existing.Id == service.Id);
        if (index >= 0)
        {
            Services[index] = service;
        }

        return Task.FromResult(service);
    }

    public Task DeactivateServiceAsync(string categoryId, string serviceId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnDeactivate)
        {
            throw new InvalidOperationException("Failed to deactivate service.");
        }

        DeactivateCalls.Add((categoryId, serviceId));
        var index = Services.FindIndex(existing => existing.Id == serviceId);
        if (index >= 0)
        {
            Services[index] = Services[index] with { Status = ServiceStatus.Discontinued };
        }

        return Task.CompletedTask;
    }
}
