using Rojan.Desktop.Application.Services;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Tests.Services;

public sealed class ServiceCommandServiceTests
{
    private static DomainServices.Service MakeService(string id = "service-1") =>
        new(id, "Haircut & Style", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Active,
            60, "$65", "Classic cut and blow-dry finish.");

    [Fact]
    public async Task AssignSpecialistAsync_ValidName_AddsAssignment()
    {
        var repository = new StubServiceRepository([MakeService()]);
        var sut = new ServiceCommandService(repository);

        var assignment = await sut.AssignSpecialistAsync("service-1", "Jordan Lee");

        Assert.Equal("service-1", assignment.ServiceId);
        Assert.Equal("Jordan Lee", assignment.SpecialistName);
        Assert.Single(repository.Assignments);
    }

    [Fact]
    public async Task UnassignSpecialistAsync_ExistingAssignment_RemovesAssignment()
    {
        var repository = new StubServiceRepository([MakeService()]);
        repository.Assignments.Add(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee"));
        var sut = new ServiceCommandService(repository);

        await sut.UnassignSpecialistAsync("service-1", "assignment-1");

        Assert.Empty(repository.Assignments);
    }
}
