using Rojan.Desktop.Presentation.Theming;
using Rojan.Desktop.Shell.Theming;

namespace Rojan.Desktop.Shell.Tests.Theming;

/// <summary>
/// Exercises <see cref="ThemeService"/> against a temp settings file (never
/// the real %LocalAppData%\RojanDesktop\theme.json) via its internal
/// path-overriding constructor - covers the "Default should be Light"
/// requirement, persistence, and restart-required semantics. System-mode
/// resolution against the live Windows registry is exercised only for
/// "doesn't throw and resolves to a real theme" - the actual OS setting is
/// environment-dependent and not something a unit test should assert a
/// specific value for.
/// </summary>
public sealed class ThemeServiceTests : IDisposable
{
    private readonly string _settingsFilePath;

    public ThemeServiceTests()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "theme.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InitializeAsync_WithNoSettingsFile_DefaultsToLight()
    {
        var service = new ThemeService(_settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal(ThemeMode.Light, service.SelectedMode);
        Assert.Equal(ThemeMode.Light, service.ResolvedTheme);
        Assert.False(service.IsRestartRequired);
    }

    [Fact]
    public async Task InitializeAsync_WithPersistedDarkMode_RestoresIt()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(_settingsFilePath, """{"mode":"Dark"}""");

        var service = new ThemeService(_settingsFilePath);
        await service.InitializeAsync();

        Assert.Equal(ThemeMode.Dark, service.SelectedMode);
        Assert.Equal(ThemeMode.Dark, service.ResolvedTheme);
    }

    [Fact]
    public async Task InitializeAsync_WithCorruptSettingsFile_FallsBackToLightWithoutThrowing()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(_settingsFilePath, "{ not valid json");

        var service = new ThemeService(_settingsFilePath);
        await service.InitializeAsync();

        Assert.Equal(ThemeMode.Light, service.SelectedMode);
    }

    [Fact]
    public async Task InitializeAsync_WithSystemMode_ResolvesToLightOrDarkWithoutThrowing()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(_settingsFilePath, """{"mode":"System"}""");

        var service = new ThemeService(_settingsFilePath);
        await service.InitializeAsync();

        Assert.Equal(ThemeMode.System, service.SelectedMode);
        Assert.True(service.ResolvedTheme is ThemeMode.Light or ThemeMode.Dark);
    }

    [Fact]
    public async Task SetThemeModeAsync_ToDifferentResolvedTheme_PersistsAndRequiresRestart()
    {
        var service = new ThemeService(_settingsFilePath);
        await service.InitializeAsync();

        await service.SetThemeModeAsync(ThemeMode.Dark);

        Assert.True(service.IsRestartRequired);
        var persisted = await File.ReadAllTextAsync(_settingsFilePath);
        Assert.Contains("Dark", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetThemeModeAsync_ToSameResolvedTheme_DoesNotRequireRestart()
    {
        var service = new ThemeService(_settingsFilePath);
        await service.InitializeAsync();

        await service.SetThemeModeAsync(ThemeMode.Light);

        Assert.False(service.IsRestartRequired);
    }
}
