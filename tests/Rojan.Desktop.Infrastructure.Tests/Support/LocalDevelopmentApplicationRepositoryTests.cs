using Rojan.Desktop.Domain.Support;
using Rojan.Desktop.Infrastructure.Support;

namespace Rojan.Desktop.Infrastructure.Tests.Support;

public sealed class LocalDevelopmentApplicationRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalDevelopmentApplicationRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "development-applications.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static DevelopmentApplication Application(string id) => new(
        id, "Sara", "Ahmadi", "0912-000-0000", "sara@example.com", "Tehran", "Backend",
        "https://github.com/sara", "https://linkedin.com/in/sara", "https://sara.dev", "https://sara.dev/resume.pdf",
        "I would like to help.", DateTimeOffset.UtcNow);

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalDevelopmentApplicationRepository(_filePath);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenGetAllAsync_RoundTrips()
    {
        var repository = new LocalDevelopmentApplicationRepository(_filePath);

        await repository.SaveAsync(Application("a1"));

        var all = await repository.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Sara", all[0].FirstName);
        Assert.Equal("Backend", all[0].CollaborationArea);
    }

    [Fact]
    public async Task SaveAsync_PersistsAcrossInstances()
    {
        var first = new LocalDevelopmentApplicationRepository(_filePath);
        await first.SaveAsync(Application("a1"));

        var second = new LocalDevelopmentApplicationRepository(_filePath);

        Assert.Single(await second.GetAllAsync());
    }
}
