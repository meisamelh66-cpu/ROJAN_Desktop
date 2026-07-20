using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

public sealed class SecretProviderTests : IDisposable
{
    private const string EnvironmentVariableName = "ROJAN_SECRET_TEST_SECRET";

    private readonly string _storageDirectory;

    public SecretProviderTests()
    {
        _storageDirectory = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "storage");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
        if (Directory.Exists(_storageDirectory))
        {
            Directory.Delete(_storageDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetSecretAsync_NeitherEnvironmentVariableNorStorageSet_ReturnsNull()
    {
        var provider = new SecretProvider(new DpapiSecureStorageService(_storageDirectory));

        var result = await provider.GetSecretAsync("test-secret");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSecretAsync_ValueOnlyInSecureStorage_ReturnsIt()
    {
        var storage = new DpapiSecureStorageService(_storageDirectory);
        await storage.SetAsync("secret:test-secret", "stored-value");
        var provider = new SecretProvider(storage);

        var result = await provider.GetSecretAsync("test-secret");

        Assert.Equal("stored-value", result);
    }

    [Fact]
    public async Task GetSecretAsync_EnvironmentVariableSet_TakesPrecedenceOverSecureStorage()
    {
        var storage = new DpapiSecureStorageService(_storageDirectory);
        await storage.SetAsync("secret:test-secret", "stored-value");
        Environment.SetEnvironmentVariable(EnvironmentVariableName, "env-value");
        var provider = new SecretProvider(storage);

        var result = await provider.GetSecretAsync("test-secret");

        Assert.Equal("env-value", result);
    }
}
