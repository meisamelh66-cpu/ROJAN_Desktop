using Rojan.Desktop.Domain.Automation;
using Rojan.Desktop.Infrastructure.Automation;

namespace Rojan.Desktop.Infrastructure.Tests.Automation;

/// <summary>Exercises <see cref="LocalBusinessRuleRepository"/> against a temp file - persistence round-trip, update, and delete.</summary>
public sealed class LocalBusinessRuleRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalBusinessRuleRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "rules.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static BusinessRule Rule(string id, int priority = 1) => new(
        id, "VIP Discount", "", [new BusinessRuleCondition("IsVip", BusinessRuleOperator.Equals, "true")],
        new BusinessRuleAction(BusinessRuleActionType.ApplyDiscount, new Dictionary<string, string> { ["percentage"] = "10" }),
        priority, IsEnabled: true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "org-1", "branch-1");

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalBusinessRuleRepository(_filePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_RoundTrips()
    {
        var repository = new LocalBusinessRuleRepository(_filePath);
        await repository.SaveAsync(Rule("r1"));

        var reloaded = await repository.GetByIdAsync("r1");

        Assert.NotNull(reloaded);
        Assert.Single(reloaded!.Conditions);
        Assert.Equal(BusinessRuleActionType.ApplyDiscount, reloaded.Action.Type);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_UpdatesRatherThanDuplicates()
    {
        var repository = new LocalBusinessRuleRepository(_filePath);
        await repository.SaveAsync(Rule("r1"));

        await repository.SaveAsync(Rule("r1") with { IsEnabled = false });

        var all = await repository.GetAllAsync();
        Assert.Single(all);
        Assert.False(all[0].IsEnabled);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRule()
    {
        var repository = new LocalBusinessRuleRepository(_filePath);
        await repository.SaveAsync(Rule("r1"));

        await repository.DeleteAsync("r1");

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_PersistsAcrossInstances()
    {
        var first = new LocalBusinessRuleRepository(_filePath);
        await first.SaveAsync(Rule("r1"));

        var second = new LocalBusinessRuleRepository(_filePath);

        Assert.Single(await second.GetAllAsync());
    }
}
