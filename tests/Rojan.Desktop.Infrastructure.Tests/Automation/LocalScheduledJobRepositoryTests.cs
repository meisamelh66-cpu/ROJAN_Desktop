using Rojan.Desktop.Domain.Automation;
using Rojan.Desktop.Infrastructure.Automation;

namespace Rojan.Desktop.Infrastructure.Tests.Automation;

/// <summary>Exercises <see cref="LocalScheduledJobRepository"/> against a temp file - persistence round-trip, update, and delete.</summary>
public sealed class LocalScheduledJobRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalScheduledJobRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "jobs.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ScheduledJob Job(string id) => new(
        id, "Nightly Sync", ScheduleFrequency.Daily, null, "workflow-1", IsEnabled: true,
        DateTimeOffset.UtcNow.AddDays(1), LastRunAt: null, "org-1", "branch-1");

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalScheduledJobRepository(_filePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_RoundTrips()
    {
        var repository = new LocalScheduledJobRepository(_filePath);
        await repository.SaveAsync(Job("j1"));

        var reloaded = await repository.GetByIdAsync("j1");

        Assert.NotNull(reloaded);
        Assert.Equal(ScheduleFrequency.Daily, reloaded!.Frequency);
        Assert.Equal("workflow-1", reloaded.WorkflowId);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_UpdatesRatherThanDuplicates()
    {
        var repository = new LocalScheduledJobRepository(_filePath);
        await repository.SaveAsync(Job("j1"));

        await repository.SaveAsync(Job("j1") with { IsEnabled = false });

        var all = await repository.GetAllAsync();
        Assert.Single(all);
        Assert.False(all[0].IsEnabled);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheJob()
    {
        var repository = new LocalScheduledJobRepository(_filePath);
        await repository.SaveAsync(Job("j1"));

        await repository.DeleteAsync("j1");

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_PersistsAcrossInstances()
    {
        var first = new LocalScheduledJobRepository(_filePath);
        await first.SaveAsync(Job("j1"));

        var second = new LocalScheduledJobRepository(_filePath);

        Assert.Single(await second.GetAllAsync());
    }
}
