using Rojan.Desktop.Domain.Support;
using Rojan.Desktop.Infrastructure.Support;

namespace Rojan.Desktop.Infrastructure.Tests.Support;

public sealed class LocalSupportMessageRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalSupportMessageRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "messages.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SupportMessage Message(string id) => new(
        id, SupportMessageType.General, "Subject", "Body", "Sara", "sara@example.com", DateTimeOffset.UtcNow);

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalSupportMessageRepository(_filePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_MultipleMessages_InsertsAtTheFront()
    {
        var repository = new LocalSupportMessageRepository(_filePath);

        await repository.SaveAsync(Message("m1"));
        await repository.SaveAsync(Message("m2"));

        var all = await repository.GetAllAsync();
        Assert.Equal(["m2", "m1"], all.Select(m => m.Id));
    }

    [Fact]
    public async Task SaveAsync_BeyondMaxEntries_EvictsTheOldest()
    {
        var repository = new LocalSupportMessageRepository(_filePath);
        for (var i = 0; i < LocalSupportMessageRepository.MaxEntries + 2; i++)
        {
            await repository.SaveAsync(Message($"m{i}"));
        }

        var all = await repository.GetAllAsync();

        Assert.Equal(LocalSupportMessageRepository.MaxEntries, all.Count);
        Assert.DoesNotContain(all, m => m.Id == "m0");
    }

    [Fact]
    public async Task SaveAsync_PersistsAcrossInstances()
    {
        var first = new LocalSupportMessageRepository(_filePath);
        await first.SaveAsync(Message("m1"));

        var second = new LocalSupportMessageRepository(_filePath);

        Assert.Single(await second.GetAllAsync());
    }
}
