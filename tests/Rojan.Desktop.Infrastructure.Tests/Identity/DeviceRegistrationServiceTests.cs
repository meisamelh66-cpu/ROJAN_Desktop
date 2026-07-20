using Rojan.Desktop.Infrastructure.Identity;

namespace Rojan.Desktop.Infrastructure.Tests.Identity;

/// <summary>Exercises <see cref="DeviceRegistrationService"/> against a temp file (never the real <c>%LocalAppData%\RojanDesktop\identity\device.json</c>) via its internal path-overriding constructor - same pattern <c>Shell.Tests.Organizations.CurrentSessionServiceTests</c> establishes.</summary>
public sealed class DeviceRegistrationServiceTests : IDisposable
{
    private readonly string _filePath;

    public DeviceRegistrationServiceTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "device.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureRegisteredAsync_FirstCall_MintsAndPersistsANewDevice()
    {
        var service = new DeviceRegistrationService(_filePath);

        var device = await service.EnsureRegisteredAsync();

        Assert.False(string.IsNullOrWhiteSpace(device.Id));
        Assert.False(string.IsNullOrWhiteSpace(device.Fingerprint));
        Assert.Equal(Environment.MachineName, device.MachineName);
        Assert.True(File.Exists(_filePath));
        Assert.NotNull(service.CurrentInstallation);
    }

    [Fact]
    public async Task EnsureRegisteredAsync_CalledTwiceOnSameInstance_ReturnsTheSameDeviceId()
    {
        var service = new DeviceRegistrationService(_filePath);

        var first = await service.EnsureRegisteredAsync();
        var second = await service.EnsureRegisteredAsync();

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task EnsureRegisteredAsync_CalledAgainAfterRestartFromPersistedFile_ReturnsTheSameDeviceAndInstallationIds()
    {
        var first = new DeviceRegistrationService(_filePath);
        var firstDevice = await first.EnsureRegisteredAsync();
        var firstInstallationId = first.CurrentInstallation!.Id;

        var second = new DeviceRegistrationService(_filePath);
        var secondDevice = await second.EnsureRegisteredAsync();

        Assert.Equal(firstDevice.Id, secondDevice.Id);
        Assert.Equal(firstInstallationId, second.CurrentInstallation!.Id);
    }

    [Fact]
    public async Task EnsureRegisteredAsync_FingerprintIsDeterministicForTheSameMachine()
    {
        var first = new DeviceRegistrationService(_filePath);
        var firstDevice = await first.EnsureRegisteredAsync();

        var otherPath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "device.json");
        try
        {
            var second = new DeviceRegistrationService(otherPath);
            var secondDevice = await second.EnsureRegisteredAsync();

            Assert.Equal(firstDevice.Fingerprint, secondDevice.Fingerprint);
        }
        finally
        {
            var directory = Path.GetDirectoryName(otherPath);
            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
