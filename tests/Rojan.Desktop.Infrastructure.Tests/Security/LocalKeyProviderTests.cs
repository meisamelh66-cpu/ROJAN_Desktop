using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

/// <summary>Backed by a real <see cref="DpapiSecureStorageService"/> over a temp directory - exercises the full generate-then-persist-then-retrieve path, not a mocked storage layer.</summary>
public sealed class LocalKeyProviderTests : IDisposable
{
    private readonly string _storageDirectory;

    public LocalKeyProviderTests()
    {
        _storageDirectory = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "storage");
    }

    public void Dispose()
    {
        if (Directory.Exists(_storageDirectory))
        {
            Directory.Delete(_storageDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_FirstCall_ReturnsA32ByteKey()
    {
        var provider = new LocalKeyProvider(new DpapiSecureStorageService(_storageDirectory));

        var key = await provider.GetOrCreateKeyAsync("sync-payload");

        Assert.Equal(32, key.Length);
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_CalledTwiceForTheSamePurpose_ReturnsTheSameKey()
    {
        var provider = new LocalKeyProvider(new DpapiSecureStorageService(_storageDirectory));

        var first = await provider.GetOrCreateKeyAsync("secure-storage");
        var second = await provider.GetOrCreateKeyAsync("secure-storage");

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_DifferentPurposes_ReturnDifferentKeys()
    {
        var provider = new LocalKeyProvider(new DpapiSecureStorageService(_storageDirectory));

        var first = await provider.GetOrCreateKeyAsync("purpose-a");
        var second = await provider.GetOrCreateKeyAsync("purpose-b");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_PersistsAcrossProviderInstances()
    {
        var storage = new DpapiSecureStorageService(_storageDirectory);
        var first = await new LocalKeyProvider(storage).GetOrCreateKeyAsync("sync-payload");

        var second = await new LocalKeyProvider(storage).GetOrCreateKeyAsync("sync-payload");

        Assert.Equal(first, second);
    }
}
