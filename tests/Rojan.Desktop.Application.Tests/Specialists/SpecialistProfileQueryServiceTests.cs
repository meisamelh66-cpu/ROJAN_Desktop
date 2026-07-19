using Rojan.Desktop.Application.Specialists;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Tests.Specialists;

public sealed class SpecialistProfileQueryServiceTests
{
    private static DomainSpecialists.Specialist MakeSpecialist(string id = "specialist-1") =>
        new(id, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "+1 555 020 1001",
            DomainSpecialists.SpecialistStatus.Active, "Specializes in balayage.");

    [Fact]
    public async Task GetProfileAsync_SpecialistExists_ReturnsSpecialistAndSkills()
    {
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Colour"));
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-2", "specialist-1", "Balayage"));
        var sut = new SpecialistProfileQueryService(repository);

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
        var sut = new SpecialistProfileQueryService(repository);

        var profile = await sut.GetProfileAsync("specialist-1");

        Assert.Empty(profile.Skills);
    }

    [Fact]
    public async Task GetProfileAsync_SpecialistDoesNotExist_ThrowsInvalidOperationException()
    {
        var repository = new StubSpecialistRepository([]);
        var sut = new SpecialistProfileQueryService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("missing-specialist"));
    }
}
