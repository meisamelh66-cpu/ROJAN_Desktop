using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Tests.Services;

/// <summary>In-memory, mutable <see cref="IServiceRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubServiceRepository : IServiceRepository
{
    public List<Service> Services { get; } = [];

    public List<SpecialistService> Assignments { get; } = [];

    public List<ServiceCategoryOption> Categories { get; } = [];

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

    public Task<Service> CreateServiceAsync(string categoryId, string name, string? description, int durationMinutes, decimal price, CancellationToken cancellationToken = default)
    {
        var category = Categories.FirstOrDefault(option => option.Id == categoryId);
        var service = new Service(
            Guid.NewGuid().ToString(),
            name,
            ServiceCategory.Other,
            ServiceStatus.Active,
            durationMinutes,
            price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            description ?? string.Empty,
            category?.Name,
            categoryId);
        Services.Add(service);
        return Task.FromResult(service);
    }

    public Task<Service> UpdateServiceAsync(
        string serviceId,
        string categoryId,
        string name,
        string? description,
        int durationMinutes,
        decimal price,
        ServiceStatus requestedStatus,
        CancellationToken cancellationToken = default)
    {
        var index = Services.FindIndex(existing => existing.Id == serviceId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Service '{serviceId}' was not found.");
        }

        var updated = Services[index] with
        {
            Name = name,
            Description = description ?? string.Empty,
            DurationMinutes = durationMinutes,
            Price = price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Status = requestedStatus,
            CategoryId = categoryId,
        };
        Services[index] = updated;
        return Task.FromResult(updated);
    }
}
