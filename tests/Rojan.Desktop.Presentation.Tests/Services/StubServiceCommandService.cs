using Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Presentation.Tests.Services;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubServiceCommandService : IServiceCommandService
{
    public List<(string ServiceId, string SpecialistName)> AssignCalls { get; } = [];

    public List<(string ServiceId, string AssignmentId)> UnassignCalls { get; } = [];

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
}
