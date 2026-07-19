using Rojan.Desktop.Application.Services;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Tests.Services;

public sealed class ServiceProfileQueryServiceTests
{
    private static DomainServices.Service MakeService(string id = "service-1") =>
        new(id, "Haircut & Style", DomainServices.ServiceCategory.Hair, DomainServices.ServiceStatus.Active,
            60, "$65", "Classic cut and blow-dry finish.");

    [Fact]
    public async Task GetProfileAsync_ServiceExists_ReturnsServiceAndAssignedSpecialists()
    {
        var repository = new StubServiceRepository([MakeService()]);
        repository.Assignments.Add(new DomainServices.SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee"));
        repository.Assignments.Add(new DomainServices.SpecialistService("assignment-2", "service-1", "specialist-2", "Priya Nair"));
        var sut = new ServiceProfileQueryService(repository);

        var profile = await sut.GetProfileAsync("service-1");

        Assert.Equal("service-1", profile.Service.Id);
        Assert.Equal(2, profile.AssignedSpecialists.Count);
        Assert.Contains(profile.AssignedSpecialists, assignment => assignment.SpecialistName == "Jordan Lee");
        Assert.Contains(profile.AssignedSpecialists, assignment => assignment.SpecialistName == "Priya Nair");
    }

    [Fact]
    public async Task GetProfileAsync_ServiceHasNoAssignments_ReturnsEmptyAssignmentsList()
    {
        var repository = new StubServiceRepository([MakeService()]);
        var sut = new ServiceProfileQueryService(repository);

        var profile = await sut.GetProfileAsync("service-1");

        Assert.Empty(profile.AssignedSpecialists);
    }

    [Fact]
    public async Task GetProfileAsync_ServiceDoesNotExist_ThrowsInvalidOperationException()
    {
        var repository = new StubServiceRepository([]);
        var sut = new ServiceProfileQueryService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("missing-service"));
    }
}
