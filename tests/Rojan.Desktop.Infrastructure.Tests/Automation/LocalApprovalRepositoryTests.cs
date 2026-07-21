using Rojan.Desktop.Domain.Automation;
using Rojan.Desktop.Infrastructure.Automation;

namespace Rojan.Desktop.Infrastructure.Tests.Automation;

/// <summary>Exercises <see cref="LocalApprovalRepository"/> against a temp file - persistence round-trip and update (this repository intentionally has no Delete, per <see cref="IApprovalRepository"/>).</summary>
public sealed class LocalApprovalRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalApprovalRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "approvals.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ApprovalRequest Request(string id) => new(
        id, ApprovalType.Leave, "Leave Request", "", "user-1", DateTimeOffset.UtcNow,
        [new ApprovalStep(0, "Manager", ApprovalStepStatus.Pending, null, null, null)],
        ApprovalStatus.Pending, CurrentStepIndex: 0, WorkflowExecutionId: null, "org-1", "branch-1");

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalApprovalRepository(_filePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_RoundTrips()
    {
        var repository = new LocalApprovalRepository(_filePath);
        await repository.SaveAsync(Request("a1"));

        var reloaded = await repository.GetByIdAsync("a1");

        Assert.NotNull(reloaded);
        Assert.Equal(ApprovalType.Leave, reloaded!.Type);
        Assert.Single(reloaded.Steps);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_UpdatesRatherThanDuplicates()
    {
        var repository = new LocalApprovalRepository(_filePath);
        await repository.SaveAsync(Request("a1"));

        await repository.SaveAsync(Request("a1") with { Status = ApprovalStatus.Approved });

        var all = await repository.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(ApprovalStatus.Approved, all[0].Status);
    }

    [Fact]
    public async Task SaveAsync_PersistsAcrossInstances()
    {
        var first = new LocalApprovalRepository(_filePath);
        await first.SaveAsync(Request("a1"));

        var second = new LocalApprovalRepository(_filePath);

        Assert.Single(await second.GetAllAsync());
    }
}
