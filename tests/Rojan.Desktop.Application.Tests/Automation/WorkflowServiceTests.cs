using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Exercises <see cref="WorkflowService"/> - CRUD plus Requirement 32.9's Draft/Published/Archived versioning and rollback.</summary>
public sealed class WorkflowServiceTests
{
    private static IReadOnlyList<WorkflowStepDto> ValidSteps()
    {
        var startId = Guid.NewGuid().ToString("N");
        var endId = Guid.NewGuid().ToString("N");
        return
        [
            AutomationTestFactory.Step(startId, WorkflowStepType.Start, endId),
            AutomationTestFactory.Step(endId, WorkflowStepType.End),
        ];
    }

    private static WorkflowService CreateService(out FakeWorkflowRepository repository)
    {
        repository = new FakeWorkflowRepository();
        return new WorkflowService(repository);
    }

    [Fact]
    public async Task CreateDraftAsync_ProducesVersion1DraftWithItselfAsParent()
    {
        var service = CreateService(out _);

        var workflow = await service.CreateDraftAsync("Welcome Flow", "desc", ValidSteps(), [], "user-1", "org-1", "branch-1");

        Assert.Equal(WorkflowStatus.Draft, workflow.Status);
        Assert.Equal(1, workflow.Version);
        Assert.Equal(workflow.Id, workflow.ParentWorkflowId);
    }

    [Fact]
    public async Task SaveDraftAsync_NonDraftWorkflow_Throws()
    {
        var service = CreateService(out _);
        var draft = await service.CreateDraftAsync("Flow", "", ValidSteps(), [], "user-1", "org-1", "branch-1");
        var published = await service.PublishAsync(draft.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveDraftAsync(published with { Name = "Renamed" }));
    }

    [Fact]
    public async Task PublishAsync_InvalidSteps_ThrowsAndDoesNotPublish()
    {
        var service = CreateService(out _);
        var invalidSteps = new[] { AutomationTestFactory.Step("only-start", WorkflowStepType.Start) };
        var draft = await service.CreateDraftAsync("Broken", "", invalidSteps, [], "user-1", "org-1", "branch-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(draft.Id));
    }

    [Fact]
    public async Task PublishAsync_ArchivesThePreviouslyPublishedVersionUnderTheSameLineage()
    {
        var service = CreateService(out _);
        var draft1 = await service.CreateDraftAsync("Flow", "", ValidSteps(), [], "user-1", "org-1", "branch-1");
        var published1 = await service.PublishAsync(draft1.Id);

        var draft2 = await service.RollbackAsync(published1.ParentWorkflowId, published1.Version, "user-1");
        var published2 = await service.PublishAsync(draft2.Id);

        var versions = await service.GetVersionsAsync(published1.ParentWorkflowId);
        Assert.Equal(WorkflowStatus.Archived, versions.Single(v => v.Id == published1.Id).Status);
        Assert.Equal(WorkflowStatus.Published, versions.Single(v => v.Id == published2.Id).Status);
    }

    [Fact]
    public async Task RollbackAsync_CreatesANewDraftWithAnIncrementedVersionNumber()
    {
        var service = CreateService(out _);
        var draft = await service.CreateDraftAsync("Flow", "", ValidSteps(), [], "user-1", "org-1", "branch-1");
        var published = await service.PublishAsync(draft.Id);

        var rolledBack = await service.RollbackAsync(published.ParentWorkflowId, published.Version, "user-2");

        Assert.Equal(WorkflowStatus.Draft, rolledBack.Status);
        Assert.Equal(2, rolledBack.Version);
        Assert.NotEqual(published.Id, rolledBack.Id);
    }

    [Fact]
    public async Task GetPublishedAsync_OnlyReturnsPublishedAndEnabledWorkflows()
    {
        var service = CreateService(out _);
        var draft = await service.CreateDraftAsync("Draft Only", "", ValidSteps(), [], "user-1", "org-1", "branch-1");
        var toPublish = await service.CreateDraftAsync("Will Publish", "", ValidSteps(), [], "user-1", "org-1", "branch-1");
        await service.PublishAsync(toPublish.Id);

        var published = await service.GetPublishedAsync();

        Assert.DoesNotContain(published, w => w.Id == draft.Id);
        Assert.Contains(published, w => w.Name == "Will Publish");
    }

    [Fact]
    public void Validate_DelegatesToDomainWorkflowRules()
    {
        var service = CreateService(out _);

        var errors = service.Validate([]);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheWorkflow()
    {
        var service = CreateService(out _);
        var draft = await service.CreateDraftAsync("Flow", "", ValidSteps(), [], "user-1", "org-1", "branch-1");

        await service.DeleteAsync(draft.Id);

        Assert.Null(await service.GetByIdAsync(draft.Id));
    }
}
