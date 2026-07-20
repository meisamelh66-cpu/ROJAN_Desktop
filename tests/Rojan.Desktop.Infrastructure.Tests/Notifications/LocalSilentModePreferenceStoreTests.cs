using Rojan.Desktop.Infrastructure.Notifications;

namespace Rojan.Desktop.Infrastructure.Tests.Notifications;

/// <summary>Exercises <see cref="LocalSilentModePreferenceStore"/> against a temp file - default value, persistence, and cross-instance round-trip.</summary>
public sealed class LocalSilentModePreferenceStoreTests : IDisposable
{
    private readonly string _filePath;

    public LocalSilentModePreferenceStoreTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "silent-mode.json");
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
    public async Task GetIsEnabledAsync_NoPersistedFile_DefaultsToFalse()
    {
        var store = new LocalSilentModePreferenceStore(_filePath);

        Assert.False(await store.GetIsEnabledAsync());
    }

    [Fact]
    public async Task SetIsEnabledAsync_PersistsTheNewValue()
    {
        var store = new LocalSilentModePreferenceStore(_filePath);

        await store.SetIsEnabledAsync(true);

        Assert.True(await store.GetIsEnabledAsync());
    }

    [Fact]
    public async Task SetIsEnabledAsync_PersistsAcrossInstances()
    {
        var first = new LocalSilentModePreferenceStore(_filePath);
        await first.SetIsEnabledAsync(true);

        var second = new LocalSilentModePreferenceStore(_filePath);

        Assert.True(await second.GetIsEnabledAsync());
    }

    [Fact]
    public async Task SetIsEnabledAsync_CanToggleBackToFalse()
    {
        var store = new LocalSilentModePreferenceStore(_filePath);
        await store.SetIsEnabledAsync(true);

        await store.SetIsEnabledAsync(false);

        Assert.False(await store.GetIsEnabledAsync());
    }
}
