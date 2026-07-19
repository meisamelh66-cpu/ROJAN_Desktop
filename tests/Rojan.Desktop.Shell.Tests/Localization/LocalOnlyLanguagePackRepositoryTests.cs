using Rojan.Desktop.Shell.Localization;

namespace Rojan.Desktop.Shell.Tests.Localization;

/// <summary>
/// Proves the Language Store Foundation's "Do NOT connect to servers yet -
/// only build the framework" boundary: an always-empty catalog, and
/// install/remove that fail loudly (<see cref="NotSupportedException"/>)
/// rather than silently no-op-ing.
/// </summary>
public sealed class LocalOnlyLanguagePackRepositoryTests
{
    [Fact]
    public async Task GetAvailableLanguagePacksAsync_ReturnsEmptyCatalog()
    {
        var repository = new LocalOnlyLanguagePackRepository();

        var catalog = await repository.GetAvailableLanguagePacksAsync();

        Assert.Empty(catalog);
    }

    [Fact]
    public async Task DownloadAndInstallAsync_ThrowsNotSupported()
    {
        var repository = new LocalOnlyLanguagePackRepository();

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.DownloadAndInstallAsync("de-DE"));
    }

    [Fact]
    public async Task RemovePackAsync_ThrowsNotSupported()
    {
        var repository = new LocalOnlyLanguagePackRepository();

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.RemovePackAsync("de-DE"));
    }
}
