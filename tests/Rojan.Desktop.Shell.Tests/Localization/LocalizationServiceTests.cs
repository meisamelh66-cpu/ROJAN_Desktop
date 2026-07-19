using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Shell.Localization;

namespace Rojan.Desktop.Shell.Tests.Localization;

/// <summary>
/// Exercises <see cref="LocalizationService"/> against a temp settings file
/// (never the real %LocalAppData%\RojanDesktop\settings.json) via its
/// internal path-overriding constructor - covers the "Default Application
/// State" (first launch = Persian), persistence, and restart-required
/// requirements.
/// </summary>
public sealed class LocalizationServiceTests : IDisposable
{
    private readonly string _settingsFilePath;

    public LocalizationServiceTests()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "settings.json");
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
    public async Task InitializeAsync_WithNoAvailableLanguagesAndNoSettingsFile_DefaultsToBuiltInPersianFallback()
    {
        var service = new LocalizationService(new StubLanguagePackManager([]), _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("fa-IR", service.CurrentLanguage.Code);
        Assert.True(service.CurrentLanguage.IsRightToLeft);
        Assert.True(service.CurrentLanguage.IsBuiltIn);
        Assert.False(service.IsRestartRequired);
    }

    [Fact]
    public async Task InitializeAsync_WithNoSettingsFile_DefaultsToPersianAmongDiscoveredLanguages()
    {
        var packManager = new StubLanguagePackManager([EnUs, FaIr, ArSa]);
        var service = new LocalizationService(packManager, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("fa-IR", service.CurrentLanguage.Code);
    }

    [Fact]
    public async Task InitializeAsync_WithPersistedLanguage_RestoresIt()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(_settingsFilePath, """{"language":"en-US"}""");

        var packManager = new StubLanguagePackManager([EnUs, FaIr, ArSa]);
        var service = new LocalizationService(packManager, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("en-US", service.CurrentLanguage.Code);
    }

    [Fact]
    public async Task InitializeAsync_WithCorruptSettingsFile_FallsBackToPersianWithoutThrowing()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(_settingsFilePath, "{ not valid json");

        var packManager = new StubLanguagePackManager([EnUs, FaIr, ArSa]);
        var service = new LocalizationService(packManager, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("fa-IR", service.CurrentLanguage.Code);
    }

    [Fact]
    public async Task SetLanguageAsync_ToDifferentLanguage_PersistsAndRequiresRestart()
    {
        var packManager = new StubLanguagePackManager([EnUs, FaIr, ArSa]);
        var service = new LocalizationService(packManager, _settingsFilePath);
        await service.InitializeAsync();

        await service.SetLanguageAsync("en-US");

        Assert.True(service.IsRestartRequired);
        var persisted = await File.ReadAllTextAsync(_settingsFilePath);
        Assert.Contains("en-US", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetLanguageAsync_ToSameLanguage_DoesNotRequireRestart()
    {
        var packManager = new StubLanguagePackManager([EnUs, FaIr, ArSa]);
        var service = new LocalizationService(packManager, _settingsFilePath);
        await service.InitializeAsync();

        await service.SetLanguageAsync("fa-IR");

        Assert.False(service.IsRestartRequired);
    }

    [Fact]
    public async Task SetLanguageAsync_ToUnknownLanguage_Throws()
    {
        var packManager = new StubLanguagePackManager([FaIr]);
        var service = new LocalizationService(packManager, _settingsFilePath);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetLanguageAsync("xx-XX"));
    }

    [Fact]
    public async Task InitializeAsync_AppliesDiscoveredPackStringOverridesToStrings()
    {
        var packManager = new StubLanguagePackManager(
            [FaIr],
            overridesByCode: new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
            {
                ["fa-IR"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["Common_Save"] = "TEST-OVERRIDE" },
            });
        var service = new LocalizationService(packManager, _settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal("TEST-OVERRIDE", Strings.Common_Save);

        Strings.SetPackOverrides(null);
    }

    private static readonly LanguageInfo FaIr = new("fa-IR", "فارسی", "Persian", true, "Vazirmatn", NumberDigits.Persian, "Toman", "Persian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo EnUs = new("en-US", "English", "English", false, "Segoe UI", NumberDigits.Latin, "Usd", "Gregorian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo ArSa = new("ar-SA", "العربية", "Arabic", true, "Segoe UI", NumberDigits.Arabic, "Usd", "Gregorian", "1.0.0", "1.0", true);

    private sealed class StubLanguagePackManager(
        IReadOnlyList<LanguageInfo> languages,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? overridesByCode = null) : ILanguagePackManager
    {
        public Task<IReadOnlyList<LanguageInfo>> DiscoverLanguagesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(languages);

        public Task<IReadOnlyDictionary<string, string>?> GetPackStringOverridesAsync(string languageCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(overridesByCode is not null && overridesByCode.TryGetValue(languageCode, out var overrides) ? overrides : null);
    }
}
