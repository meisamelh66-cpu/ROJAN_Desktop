using Rojan.Desktop.Domain.Automation;
using Rojan.Desktop.Infrastructure.Automation;

namespace Rojan.Desktop.Infrastructure.Tests.Automation;

/// <summary>Exercises <see cref="LocalWorkflowRepository"/> against a temp file - persistence round-trip across every workflow version and lineage queries.</summary>
public sealed class LocalWorkflowRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalWorkflowRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "workflows.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static WorkflowDefinition Workflow(string id, string parentId, int version, WorkflowStatus status = WorkflowStatus.Draft) =>
        new(id, parentId, "Flow", "", [], [], status, version, IsEnabled: true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "user-1", "org-1", "branch-1");

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalWorkflowRepository(_filePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_RoundTrips()
    {
        var repository = new LocalWorkflowRepository(_filePath);
        var workflow = Workflow("w1", "w1", 1);

        await repository.SaveAsync(workflow);
        var reloaded = await repository.GetByIdAsync("w1");

        Assert.NotNull(reloaded);
        Assert.Equal(workflow.Name, reloaded!.Name);
        Assert.Equal(WorkflowStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_UpdatesRatherThanDuplicates()
    {
        var repository = new LocalWorkflowRepository(_filePath);
        await repository.SaveAsync(Workflow("w1", "w1", 1));

        await repository.SaveAsync(Workflow("w1", "w1", 1) with { Status = WorkflowStatus.Published });

        var all = await repository.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(WorkflowStatus.Published, all[0].Status);
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsOnlyTheMatchingLineageOrderedByVersionDescending()
    {
        var repository = new LocalWorkflowRepository(_filePath);
        await repository.SaveAsync(Workflow("w1", "parent-1", 1));
        await repository.SaveAsync(Workflow("w2", "parent-1", 2));
        await repository.SaveAsync(Workflow("w3", "parent-2", 1));

        var versions = await repository.GetVersionsAsync("parent-1");

        Assert.Equal(["w2", "w1"], versions.Select(w => w.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheWorkflow()
    {
        var repository = new LocalWorkflowRepository(_filePath);
        await repository.SaveAsync(Workflow("w1", "w1", 1));

        await repository.DeleteAsync("w1");

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_PersistsAcrossInstances()
    {
        var first = new LocalWorkflowRepository(_filePath);
        await first.SaveAsync(Workflow("w1", "w1", 1));

        var second = new LocalWorkflowRepository(_filePath);

        Assert.Single(await second.GetAllAsync());
    }
}
