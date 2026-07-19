using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Tests.Services;

/// <summary>In-memory, mutable <see cref="IServiceRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubServiceRepository : IServiceRepository
{
    public List<Service> Services { get; } = [];

    public List<SpecialistService> Assignments { get; } = [];

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
}
