using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Shell.Localization;

namespace Rojan.Desktop.Shell.Tests.Localization;

/// <summary>
/// Proves the "application must automatically detect installed packs"
/// requirement against a temp directory (never the real Languages/ folder
/// next to the test host) via <see cref="LanguagePackManager"/>'s internal
/// path-overriding constructor.
/// </summary>
public sealed class LanguagePackManagerTests : IDisposable
{
    private readonly string _languagesDirectory;

    public LanguagePackManagerTests()
    {
        _languagesDirectory = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_languagesDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_languagesDirectory))
        {
            Directory.Delete(_languagesDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverLanguagesAsync_WhenDirectoryMissing_ReturnsEmptyList()
    {
        var manager = new LanguagePackManager(Path.Combine(_languagesDirectory, "does-not-exist"));

        var languages = await manager.DiscoverLanguagesAsync();

        Assert.Empty(languages);
    }

    [Fact]
    public async Task DiscoverLanguagesAsync_ParsesEveryWellFormedPackFile()
    {
        WritePack("fa-IR.pack", ValidPackJson("fa-IR", "فارسی", "Persian", isRightToLeft: true, isBuiltIn: true));
        WritePack("en-US.pack", ValidPackJson("en-US", "English", "English", isRightToLeft: false, isBuiltIn: true));

        var manager = new LanguagePackManager(_languagesDirectory);

        var languages = await manager.DiscoverLanguagesAsync();

        Assert.Equal(2, languages.Count);
        Assert.Contains(languages, l => l.Code == "fa-IR" && l.IsRightToLeft && l.IsBuiltIn);
        Assert.Contains(languages, l => l.Code == "en-US" && !l.IsRightToLeft && l.IsBuiltIn);
    }

    [Fact]
    public async Task DiscoverLanguagesAsync_SkipsMalformedPackFile_WithoutThrowing()
    {
        WritePack("fa-IR.pack", ValidPackJson("fa-IR", "فارسی", "Persian", isRightToLeft: true, isBuiltIn: true));
        WritePack("broken.pack", "{ this is not valid json");

        var manager = new LanguagePackManager(_languagesDirectory);

        var languages = await manager.DiscoverLanguagesAsync();

        Assert.Single(languages);
        Assert.Equal("fa-IR", languages[0].Code);
    }

    [Fact]
    public async Task DiscoverLanguagesAsync_SkipsPackWithoutCode()
    {
        WritePack("no-code.pack", ValidPackJson(string.Empty, "Nowhere", "Nowhere", isRightToLeft: false, isBuiltIn: false));

        var manager = new LanguagePackManager(_languagesDirectory);

        var languages = await manager.DiscoverLanguagesAsync();

        Assert.Empty(languages);
    }

    [Fact]
    public async Task GetPackStringOverridesAsync_WhenPackDefinesStrings_ReturnsThem()
    {
        WritePack("de-DE.pack", """
            {
              "code": "de-DE",
              "nativeName": "Deutsch",
              "englishName": "German",
              "isRightToLeft": false,
              "fontFamily": "Segoe UI",
              "numberDigits": "Latin",
              "defaultCurrencyCode": "Eur",
              "dateProviderId": "Gregorian",
              "packVersion": "1.0.0",
              "compatibilityVersion": "1.0",
              "isBuiltIn": false,
              "strings": {
                "Dashboard_Title": "Instrumententafel"
              }
            }
            """);

        var manager = new LanguagePackManager(_languagesDirectory);

        var overrides = await manager.GetPackStringOverridesAsync("de-DE");

        Assert.NotNull(overrides);
        Assert.Equal("Instrumententafel", overrides["Dashboard_Title"]);
    }

    [Fact]
    public async Task GetPackStringOverridesAsync_WhenPackFileMissing_ReturnsNull()
    {
        var manager = new LanguagePackManager(_languagesDirectory);

        var overrides = await manager.GetPackStringOverridesAsync("xx-XX");

        Assert.Null(overrides);
    }

    private void WritePack(string fileName, string json) =>
        File.WriteAllText(Path.Combine(_languagesDirectory, fileName), json);

    private static string ValidPackJson(string code, string nativeName, string englishName, bool isRightToLeft, bool isBuiltIn) =>
        $$"""
        {
          "code": "{{code}}",
          "nativeName": "{{nativeName}}",
          "englishName": "{{englishName}}",
          "isRightToLeft": {{(isRightToLeft ? "true" : "false")}},
          "fontFamily": "Segoe UI",
          "numberDigits": "Latin",
          "defaultCurrencyCode": "Usd",
          "dateProviderId": "Gregorian",
          "packVersion": "1.0.0",
          "compatibilityVersion": "1.0",
          "isBuiltIn": {{(isBuiltIn ? "true" : "false")}}
        }
        """;
}
