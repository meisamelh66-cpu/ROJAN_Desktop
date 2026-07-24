using Rojan.Desktop.Application.Specialists;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Tests.Specialists;

public sealed class SpecialistCommandServiceTests
{
    private static DomainSpecialists.Specialist MakeSpecialist(string id = "specialist-1") =>
        new(id, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "+1 555 020 1001",
            DomainSpecialists.SpecialistStatus.Active, "Specializes in balayage.");

    [Fact]
    public async Task CreateSpecialistAsync_ValidRequest_AddsSpecialistAsActive()
    {
        var repository = new StubSpecialistRepository();
        var sut = new SpecialistCommandService(repository);
        var request = new CreateSpecialistRequest("Riley Chen", "Junior Stylist", "riley.chen@rojan.example", "555-0100", "New hire");

        var created = await sut.CreateSpecialistAsync(request);

        Assert.Equal("Riley Chen", created.FullName);
        Assert.Equal(SpecialistStatus.Active, created.Status);
        Assert.Single(repository.Specialists);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_ValidRequest_ReplacesSpecialistFields()
    {
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        var sut = new SpecialistCommandService(repository);
        var request = new UpdateSpecialistRequest("specialist-1", "Jordan Lee", "Lead Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.OnLeave, "Promoted to lead.");

        var updated = await sut.UpdateSpecialistAsync(request);

        Assert.Equal(SpecialistStatus.OnLeave, updated.Status);
        Assert.Equal("Lead Colour Specialist", updated.Title);
        Assert.Equal(DomainSpecialists.SpecialistStatus.OnLeave, Assert.Single(repository.Specialists).Status);
    }

    [Fact]
    public async Task AddSkillAsync_ValidName_AddsSkill()
    {
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        var sut = new SpecialistCommandService(repository);

        var skill = await sut.AddSkillAsync("specialist-1", "Massage");

        Assert.Equal("Massage", skill.Name);
        Assert.Single(repository.Skills);
    }

    [Fact]
    public async Task RemoveSkillAsync_ExistingSkill_RemovesSkill()
    {
        var repository = new StubSpecialistRepository([MakeSpecialist()]);
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Colour"));
        var sut = new SpecialistCommandService(repository);

        await sut.RemoveSkillAsync("specialist-1", "skill-1");

        Assert.Empty(repository.Skills);
    }

    // Sprint 5 Commit 3: specialist lifecycle rules.

    private static DomainSpecialists.Specialist MakeSpecialistWithStatus(DomainSpecialists.SpecialistStatus status, string id = "specialist-1") =>
        new(id, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "+1 555 020 1001",
            status, "Specializes in balayage.");

    [Fact]
    public async Task UpdateSpecialistAsync_ValidStatusTransition_UpdatesStatus()
    {
        var repository = new StubSpecialistRepository([MakeSpecialistWithStatus(DomainSpecialists.SpecialistStatus.Active)]);
        var sut = new SpecialistCommandService(repository);
        var request = new UpdateSpecialistRequest("specialist-1", "Jordan Lee", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.OnLeave, "Specializes in balayage.");

        var updated = await sut.UpdateSpecialistAsync(request);

        Assert.Equal(SpecialistStatus.OnLeave, updated.Status);
        Assert.Equal(DomainSpecialists.SpecialistStatus.OnLeave, Assert.Single(repository.Specialists).Status);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_InvalidStatusTransition_ThrowsInvalidOperationException()
    {
        // OnLeave -> Inactive is legal, but Inactive -> OnLeave is not (an archived specialist must
        // be reactivated first, not moved straight to a temporary-pause status).
        var repository = new StubSpecialistRepository([MakeSpecialistWithStatus(DomainSpecialists.SpecialistStatus.Inactive)]);
        var sut = new SpecialistCommandService(repository);
        var request = new UpdateSpecialistRequest("specialist-1", "Jordan Lee", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.OnLeave, "Specializes in balayage.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateSpecialistAsync(request));

        // "Failed update keeps original specialist intact" - status must not have changed.
        Assert.Equal(DomainSpecialists.SpecialistStatus.Inactive, Assert.Single(repository.Specialists).Status);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_SameStatus_DoesNotThrowAndUpdatesOtherFields()
    {
        // The common case: editing name/title/email/phone/bio without touching status at all must
        // keep working exactly as before - this is not a "transition".
        var repository = new StubSpecialistRepository([MakeSpecialistWithStatus(DomainSpecialists.SpecialistStatus.Active)]);
        var sut = new SpecialistCommandService(repository);
        var request = new UpdateSpecialistRequest("specialist-1", "Jordan Lee-Chen", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.Active, "Updated bio.");

        var updated = await sut.UpdateSpecialistAsync(request);

        Assert.Equal("Jordan Lee-Chen", updated.FullName);
        Assert.Equal("Updated bio.", updated.Bio);
        Assert.Equal(SpecialistStatus.Active, updated.Status);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_SpecialistDoesNotExist_ThrowsInvalidOperationException()
    {
        var repository = new StubSpecialistRepository([]);
        var sut = new SpecialistCommandService(repository);
        var request = new UpdateSpecialistRequest("missing-specialist", "Jordan Lee", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.Active, "Specializes in balayage.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateSpecialistAsync(request));
    }
}
