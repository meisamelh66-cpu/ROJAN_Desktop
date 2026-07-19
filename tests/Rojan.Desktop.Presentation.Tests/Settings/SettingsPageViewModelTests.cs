using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Settings;

namespace Rojan.Desktop.Presentation.Tests.Settings;

public sealed class SettingsPageViewModelTests
{
    private static readonly LanguageInfo FaIr = new("fa-IR", "فارسی", "Persian", true, "Vazirmatn", NumberDigits.Persian, "Toman", "Persian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo EnUs = new("en-US", "English", "English", false, "Segoe UI", NumberDigits.Latin, "Usd", "Gregorian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo DeDe = new("de-DE", "Deutsch", "German", false, "Segoe UI", NumberDigits.Latin, "Eur", "Gregorian", "1.0.0", "1.0", false);

    [Fact]
    public void Constructor_SplitsAvailableLanguagesIntoBuiltInAndInstalledPacks()
    {
        var localizationService = new StubLocalizationService([FaIr, EnUs, DeDe], FaIr);

        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository());

        Assert.Equal(2, sut.BuiltInLanguages.Count);
        Assert.Contains(sut.BuiltInLanguages, l => l.Code == "fa-IR");
        Assert.Contains(sut.BuiltInLanguages, l => l.Code == "en-US");
        Assert.Single(sut.InstalledPacks);
        Assert.Equal("de-DE", sut.InstalledPacks[0].Code);
    }

    [Fact]
    public void Constructor_PreselectsCurrentLanguage()
    {
        var localizationService = new StubLocalizationService([FaIr, EnUs], EnUs);

        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository());

        Assert.Equal("en-US", sut.SelectedLanguage?.Code);
    }

    [Fact]
    public void Constructor_LoadsAvailableLanguagePacksFromRepository()
    {
        var catalog = new[] { new LanguagePackCatalogEntry("fr-FR", "Français", "French", "1.0.0", IsInstalled: false) };
        var localizationService = new StubLocalizationService([FaIr], FaIr);

        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(catalog));

        Assert.Single(sut.AvailableLanguagePacks);
        Assert.Equal("fr-FR", sut.AvailableLanguagePacks[0].Code);
    }

    [Fact]
    public void ApplyLanguageCommand_CanExecute_FalseWhenNoLanguageSelected()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository())
        {
            SelectedLanguage = null,
        };

        Assert.False(sut.ApplyLanguageCommand.CanExecute(null));
    }

    [Fact]
    public void ApplyLanguageCommand_ToDifferentLanguage_PersistsAndSetsRestartRequiredStatus()
    {
        var localizationService = new StubLocalizationService([FaIr, EnUs], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository())
        {
            SelectedLanguage = EnUs,
        };

        sut.ApplyLanguageCommand.Execute(null);

        Assert.Equal("en-US", localizationService.LastSetLanguageCode);
        Assert.True(sut.IsRestartRequired);
        Assert.Equal(Strings.Settings_Language_RestartRequired, sut.StatusMessage);
    }

    [Fact]
    public void ApplyLanguageCommand_ToSameLanguage_LeavesStatusMessageEmpty()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository())
        {
            SelectedLanguage = FaIr,
        };

        sut.ApplyLanguageCommand.Execute(null);

        Assert.False(sut.IsRestartRequired);
        Assert.Equal(string.Empty, sut.StatusMessage);
    }

    [Fact]
    public void DownloadOrInstallCommand_RepositoryNotSupported_SurfacesMessageInsteadOfThrowing()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository());
        var entry = new LanguagePackCatalogEntry("fr-FR", "Français", "French", "1.0.0", IsInstalled: false);

        sut.DownloadOrInstallCommand.Execute(entry);

        Assert.False(string.IsNullOrEmpty(sut.StatusMessage));
    }

    [Fact]
    public void RemovePackCommand_RepositoryNotSupported_SurfacesMessageInsteadOfThrowing()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository());

        sut.RemovePackCommand.Execute(DeDe);

        Assert.False(string.IsNullOrEmpty(sut.StatusMessage));
    }

    [Fact]
    public void CurrentLanguageDisplay_IncludesCurrentLanguageNativeName()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository());

        Assert.Contains(FaIr.NativeName, sut.CurrentLanguageDisplay, StringComparison.Ordinal);
    }
}
