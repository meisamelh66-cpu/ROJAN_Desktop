using Rojan.Desktop.Domain.Automation;
using Rojan.Desktop.Infrastructure.Automation;

namespace Rojan.Desktop.Infrastructure.Tests.Automation;

/// <summary>Exercises <see cref="LocalWorkflowExecutionRepository"/> against a temp file - persistence round-trip, newest-first insertion, and the <see cref="LocalWorkflowExecutionRepository.MaxEntries"/> eviction cap (Requirement 32.8's bounded execution history).</summary>
public sealed class LocalWorkflowExecutionRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalWorkflowExecutionRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "executions.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static WorkflowExecution Execution(string id) => new(
        id, "workflow-1", 1, "Flow", WorkflowExecutionStatus.Completed, null, "user-1",
        [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DurationMs: 10, null, "org-1", "branch-1");

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalWorkflowExecutionRepository(_filePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_NewEntries_InsertsAtTheFront()
    {
        var repository = new LocalWorkflowExecutionRepository(_filePath);

        await repository.SaveAsync(Execution("e1"));
        await repository.SaveAsync(Execution("e2"));

        var all = await repository.GetAllAsync();
        Assert.Equal(["e2", "e1"], all.Select(e => e.Id));
    }

    [Fact]
    public async Task SaveAsync_ExistingId_UpdatesInPlaceRatherThanReinserting()
    {
        var repository = new LocalWorkflowExecutionRepository(_filePath);
        await repository.SaveAsync(Execution("e1"));
        await repository.SaveAsync(Execution("e2"));

        await repository.SaveAsync(Execution("e1") with { Status = WorkflowExecutionStatus.Failed });

        var all = await repository.GetAllAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(WorkflowExecutionStatus.Failed, all.Single(e => e.Id == "e1").Status);
    }

    [Fact]
    public async Task SaveAsync_BeyondMaxEntries_EvictsTheOldest()
    {
        var repository = new LocalWorkflowExecutionRepository(_filePath);
        for (var i = 0; i < LocalWorkflowExecutionRepository.MaxEntries + 2; i++)
        {
            await repository.SaveAsync(Execution($"e{i}"));
        }

        var all = await repository.GetAllAsync();

        Assert.Equal(LocalWorkflowExecutionRepository.MaxEntries, all.Count);
        Assert.DoesNotContain(all, e => e.Id == "e0");
        Assert.Contains(all, e => e.Id == $"e{LocalWorkflowExecutionRepository.MaxEntries + 1}");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var repository = new LocalWorkflowExecutionRepository(_filePath);

        Assert.Null(await repository.GetByIdAsync("missing"));
    }

    [Fact]
    public async Task SaveAsync_PersistsAcrossInstances()
    {
        var first = new LocalWorkflowExecutionRepository(_filePath);
        await first.SaveAsync(Execution("e1"));

        var second = new LocalWorkflowExecutionRepository(_filePath);

        Assert.Single(await second.GetAllAsync());
    }
}
