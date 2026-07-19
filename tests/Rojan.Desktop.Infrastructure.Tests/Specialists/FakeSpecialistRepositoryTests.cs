using Rojan.Desktop.Domain.Specialists;
using Rojan.Desktop.Infrastructure.Specialists;

namespace Rojan.Desktop.Infrastructure.Tests.Specialists;

/// <summary>Smoke + behavioral coverage - same reasoning as Customers.FakeCustomerRepositoryTests.</summary>
public sealed class FakeSpecialistRepositoryTests
{
    [Fact]
    public async Task GetSpecialistsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeSpecialistRepository();

        var result = await sut.GetSpecialistsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetSpecialistsAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeSpecialistRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetSpecialistsAsync(cts.Token));
    }

    [Fact]
    public async Task GetSpecialistByIdAsync_KnownId_ReturnsMatchingSpecialist()
    {
        var sut = new FakeSpecialistRepository();

        var specialist = await sut.GetSpecialistByIdAsync("specialist-1");

        Assert.NotNull(specialist);
        Assert.Equal("specialist-1", specialist.Id);
    }

    [Fact]
    public async Task GetSpecialistByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = new FakeSpecialistRepository();

        var specialist = await sut.GetSpecialistByIdAsync("no-such-specialist");

        Assert.Null(specialist);
    }

    [Fact]
    public async Task GetSkillsAsync_KnownSpecialist_ReturnsOnlyThatSpecialistsSkills()
    {
        var sut = new FakeSpecialistRepository();

        var skills = await sut.GetSkillsAsync("specialist-1");

        Assert.NotEmpty(skills);
        Assert.All(skills, skill => Assert.Equal("specialist-1", skill.SpecialistId));
    }

    [Fact]
    public async Task CreateSpecialistAsync_NewSpecialist_BecomesVisibleViaGetSpecialistsAsync()
    {
        var sut = new FakeSpecialistRepository();
        var newSpecialist = new Specialist("specialist-new", "Test Specialist", string.Empty, "test@example.com",
            string.Empty, SpecialistStatus.Active, string.Empty);

        await sut.CreateSpecialistAsync(newSpecialist);
        var specialists = await sut.GetSpecialistsAsync();

        Assert.Contains(specialists, specialist => specialist.Id == "specialist-new");
    }

    [Fact]
    public async Task UpdateSpecialistAsync_ExistingSpecialist_ReplacesStoredRecord()
    {
        var sut = new FakeSpecialistRepository();
        var existing = await sut.GetSpecialistByIdAsync("specialist-1");
        var updated = existing! with { Status = SpecialistStatus.Inactive };

        await sut.UpdateSpecialistAsync(updated);
        var reloaded = await sut.GetSpecialistByIdAsync("specialist-1");

        Assert.Equal(SpecialistStatus.Inactive, reloaded!.Status);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_UnknownSpecialist_ThrowsInvalidOperationException()
    {
        var sut = new FakeSpecialistRepository();
        var unknown = new Specialist("no-such-specialist", "Ghost", string.Empty, string.Empty, string.Empty,
            SpecialistStatus.Active, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateSpecialistAsync(unknown));
    }

    [Fact]
    public async Task AddSkillAsync_NewSkill_BecomesVisibleViaGetSkillsAsync()
    {
        var sut = new FakeSpecialistRepository();
        var skill = new SpecialistSkill("skill-new", "specialist-1", "New Skill");

        await sut.AddSkillAsync(skill);
        var skills = await sut.GetSkillsAsync("specialist-1");

        Assert.Contains(skills, s => s.Id == "skill-new");
    }

    [Fact]
    public async Task RemoveSkillAsync_ExistingSkill_NoLongerReturnedByGetSkillsAsync()
    {
        var sut = new FakeSpecialistRepository();
        var skill = new SpecialistSkill("skill-new", "specialist-1", "New Skill");
        await sut.AddSkillAsync(skill);

        await sut.RemoveSkillAsync("specialist-1", "skill-new");
        var skills = await sut.GetSkillsAsync("specialist-1");

        Assert.DoesNotContain(skills, s => s.Id == "skill-new");
    }
}
