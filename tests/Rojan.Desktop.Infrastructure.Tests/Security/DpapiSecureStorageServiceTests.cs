using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

/// <summary>Exercises <see cref="DpapiSecureStorageService"/> against a temp directory - real Windows DPAPI encryption round-trip (CurrentUser scope), never the real <c>%LocalAppData%\RojanDesktop\security\storage\</c>.</summary>
public sealed class DpapiSecureStorageServiceTests : IDisposable
{
    private readonly string _storageDirectory;

    public DpapiSecureStorageServiceTests()
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
    public async Task SetAsync_ThenGetAsync_RoundTripsThePlaintextValue()
    {
        var service = new DpapiSecureStorageService(_storageDirectory);

        await service.SetAsync("refresh-token", "super-secret-value");
        var result = await service.GetAsync("refresh-token");

        Assert.Equal("super-secret-value", result);
    }

    [Fact]
    public async Task SetAsync_PersistsCiphertextNotPlaintextOnDisk()
    {
        var service = new DpapiSecureStorageService(_storageDirectory);

        await service.SetAsync("secret-key", "super-secret-value");
        var files = Directory.GetFiles(_storageDirectory);

        Assert.Single(files);
        var rawBytes = File.ReadAllBytes(files[0]);
        Assert.DoesNotContain("super-secret-value", System.Text.Encoding.UTF8.GetString(rawBytes));
    }

    [Fact]
    public async Task GetAsync_KeyNeverSet_ReturnsNull()
    {
        var service = new DpapiSecureStorageService(_storageDirectory);

        var result = await service.GetAsync("never-set");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_RemovesTheValue()
    {
        var service = new DpapiSecureStorageService(_storageDirectory);
        await service.SetAsync("to-remove", "value");

        await service.RemoveAsync("to-remove");
        var result = await service.GetAsync("to-remove");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_CalledTwiceForTheSameKey_OverwritesThePreviousValue()
    {
        var service = new DpapiSecureStorageService(_storageDirectory);

        await service.SetAsync("key", "first");
        await service.SetAsync("key", "second");
        var result = await service.GetAsync("key");

        Assert.Equal("second", result);
    }
}
