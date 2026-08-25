using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Application.Tests.Intelligence;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Tests.Specialists;

public sealed class SpecialistProfileQueryServiceTests
{
    private static DomainSpecialists.Specialist MakeSpecialist(string id = "specialist-1") =>
        new(id, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "+1 555 020 1001",
            DomainSpecialists.SpecialistStatus.Active, "Specializes in balayage.");

    private static StubServiceQueryService MakeServiceQueryService(IReadOnlyList<ServiceDto>? catalog = null) =>
        new(catalog ?? []);

    [Fact]
    public async Task GetProfileAsync_SpecialistExists_ReturnsSpecialistAndSkills()
    {
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Colour"));
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-2", "specialist-1", "Balayage"));
        var sut = new SpecialistProfileQueryService(repository, MakeServiceQueryService());

        var profile = await sut.GetProfileAsync("specialist-1");

        Assert.Equal("specialist-1", profile.Specialist.Id);
        Assert.Equal(2, profile.Skills.Count);
        Assert.Contains(profile.Skills, skill => skill.Name == "Colour");
        Assert.Contains(profile.Skills, skill => skill.Name == "Balayage");
    }

    [Fact]
    public async Task GetProfileAsync_SpecialistHasNoSkills_ReturnsEmptySkillsList()
    {
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        var sut = new SpecialistProfileQueryService(repository, MakeServiceQueryService());

        var profile = await sut.GetProfileAsync("specialist-1");

        Assert.Empty(profile.Skills);
    }

    [Fact]
    public async Task GetProfileAsync_SpecialistDoesNotExist_ThrowsInvalidOperationException()
    {
        var repository = new StubSpecialistRepository([]);
        var sut = new SpecialistProfileQueryService(repository, MakeServiceQueryService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("missing-specialist"));
    }

    // Specialist-Service Assignment.

    [Fact]
    public async Task GetProfileAsync_NoAssignedServices_ReturnsEmptyAssignedServicesList()
    {
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        var sut = new SpecialistProfileQueryService(repository, MakeServiceQueryService());

        var profile = await sut.GetProfileAsync("specialist-1");

        Assert.Empty(profile.AssignedServices);
    }

    [Fact]
    public async Task GetProfileAsync_HasAssignedServices_ResolvesRealNamesFromCatalog()
    {
        // The repository (ROJAN_Backend's own shape) returns ids only - this query service is the layer
        // responsible for resolving each one to its real catalog name.
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        repository.ServiceAssignments.Add(("specialist-1", "service-1"));
        repository.ServiceAssignments.Add(("specialist-1", "service-2"));
        var catalog = new List<ServiceDto>
        {
            new("service-1", "Balayage", ServiceCategory.Colour, ServiceStatus.Active, 90, "1,500,000 تومان", "Creative colour."),
            new("service-2", "Haircut", ServiceCategory.Hair, ServiceStatus.Active, 45, "500,000 تومان", "Classic cut."),
        };
        var sut = new SpecialistProfileQueryService(repository, MakeServiceQueryService(catalog));

        var profile = await sut.GetProfileAsync("specialist-1");

        Assert.Equal(2, profile.AssignedServices.Count);
        Assert.Contains(profile.AssignedServices, assignment => assignment.ServiceId == "service-1" && assignment.ServiceName == "Balayage");
        Assert.Contains(profile.AssignedServices, assignment => assignment.ServiceId == "service-2" && assignment.ServiceName == "Haircut");
    }

    [Fact]
    public async Task GetProfileAsync_AssignedServiceHasNoCatalogMatch_FallsBackToRawId()
    {
        // Honest, not lossy: an assignment whose service was since removed from the catalog is still a
        // real assignment - only its display name is stale, the row itself must never be dropped.
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        repository.ServiceAssignments.Add(("specialist-1", "deleted-service-id"));
        var sut = new SpecialistProfileQueryService(repository, MakeServiceQueryService([]));

        var profile = await sut.GetProfileAsync("specialist-1");

        var assignment = Assert.Single(profile.AssignedServices);
        Assert.Equal("deleted-service-id", assignment.ServiceId);
        Assert.Equal("deleted-service-id", assignment.ServiceName);
    }
}
