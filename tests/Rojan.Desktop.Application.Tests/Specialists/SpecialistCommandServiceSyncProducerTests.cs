using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Application.Tests.Security;
using Rojan.Desktop.Domain.Security;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Tests.Specialists;

/// <summary>
/// Exercises <see cref="SpecialistCommandServiceSyncProducer"/> - same
/// coverage shape as <c>Customers.CustomerCommandServiceSyncProducerTests</c>.
/// </summary>
public sealed class SpecialistCommandServiceSyncProducerTests
{
    private static DomainSpecialists.Specialist MakeSpecialist(
        string id = "specialist-1", DomainSpecialists.SpecialistStatus status = DomainSpecialists.SpecialistStatus.Active) =>
        new(id, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "+1 555 020 1001", status, "Specializes in balayage.");

    private static (SpecialistCommandServiceSyncProducer Sut, StubSpecialistRepository Repository, FakeSyncQueueService Queue) CreateSut(
        IReadOnlyList<DomainSpecialists.Specialist>? seed = null)
    {
        var repository = seed is null ? new StubSpecialistRepository() : new StubSpecialistRepository(seed);
        var inner = new SpecialistCommandService(repository);
        var queue = new FakeSyncQueueService();
        return (new SpecialistCommandServiceSyncProducer(inner, queue), repository, queue);
    }

    [Fact]
    public async Task CreateSpecialistAsync_Succeeds_EnqueuesOneCreateSyncOperationWithCorrectEntityInformation()
    {
        var (sut, _, queue) = CreateSut();
        var request = new CreateSpecialistRequest("Riley Chen", "Junior Stylist", "riley.chen@rojan.example", "555-0100", "New hire");

        var created = await sut.CreateSpecialistAsync(request);

        var operation = Assert.Single(queue.Enqueued);
        Assert.Equal("Specialist", operation.EntityType);
        Assert.Equal(created.Id, operation.EntityId);
        Assert.Equal("Create", operation.OperationType);
        Assert.Contains(created.Id, operation.Payload);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_Succeeds_EnqueuesOneUpdateSyncOperation()
    {
        var (sut, _, queue) = CreateSut([MakeSpecialist()]);
        var request = new UpdateSpecialistRequest("specialist-1", "Jordan Lee", "Lead Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.Active, "Promoted to lead.");

        var updated = await sut.UpdateSpecialistAsync(request);

        var operation = Assert.Single(queue.Enqueued);
        Assert.Equal("Specialist", operation.EntityType);
        Assert.Equal(updated.Id, operation.EntityId);
        Assert.Equal("Update", operation.OperationType);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_SpecialistDoesNotExist_ThrowsAndEnqueuesNothing()
    {
        var (sut, _, queue) = CreateSut();
        var request = new UpdateSpecialistRequest("missing-specialist", "Jordan Lee", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.Active, "Specializes in balayage.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateSpecialistAsync(request));

        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_InvalidStatusTransition_ThrowsAndEnqueuesNothing()
    {
        // Inactive -> OnLeave is not a valid direct transition (see DomainSpecialists.SpecialistRules).
        var (sut, _, queue) = CreateSut([MakeSpecialist(status: DomainSpecialists.SpecialistStatus.Inactive)]);
        var request = new UpdateSpecialistRequest("specialist-1", "Jordan Lee", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.OnLeave, "Specializes in balayage.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateSpecialistAsync(request));

        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task MultipleUpdates_EachSucceeds_EnqueuesOneOperationPerUpdateInOrder()
    {
        var (sut, _, queue) = CreateSut([MakeSpecialist()]);
        var first = new UpdateSpecialistRequest("specialist-1", "Jordan Lee", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.Active, "First update");
        var second = new UpdateSpecialistRequest("specialist-1", "Jordan Lee", "Senior Colour Specialist",
            "jordan.lee@rojan.example", "+1 555 020 1001", SpecialistStatus.Active, "Second update");

        await sut.UpdateSpecialistAsync(first);
        await sut.UpdateSpecialistAsync(second);

        Assert.Equal(2, queue.Enqueued.Count);
        Assert.All(queue.Enqueued, operation => Assert.Equal("Update", operation.OperationType));
        Assert.NotEqual(queue.Enqueued[0].Id, queue.Enqueued[1].Id);
    }

    [Fact]
    public async Task AddSkillAsync_IsOutOfProducerScope_NeverEnqueuesASyncOperation()
    {
        var (sut, _, queue) = CreateSut([MakeSpecialist()]);

        await sut.AddSkillAsync("specialist-1", "Massage");

        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task AssignServiceAsync_IsOutOfProducerScope_NeverEnqueuesASyncOperation()
    {
        // Specialist-Service Assignment - same "pass straight through unchanged" scope as skills above.
        var (sut, _, queue) = CreateSut([MakeSpecialist()]);

        await sut.AssignServiceAsync("specialist-1", "service-1");

        Assert.Empty(queue.Enqueued);
    }
}
