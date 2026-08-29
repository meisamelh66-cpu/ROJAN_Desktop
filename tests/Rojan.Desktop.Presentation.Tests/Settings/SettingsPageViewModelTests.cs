using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.Theming;
using Rojan.Desktop.Presentation.ViewModels.Settings;

namespace Rojan.Desktop.Presentation.Tests.Settings;

public sealed class SettingsPageViewModelTests
{
    private const string Secret = "https://internal-vpn.rojan.local/api-SECRET-token";

    private static readonly LanguageInfo FaIr = new("fa-IR", "فارسی", "Persian", true, "Vazirmatn", NumberDigits.Persian, "Toman", "Persian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo EnUs = new("en-US", "English", "English", false, "Segoe UI", NumberDigits.Latin, "Usd", "Gregorian", "1.0.0", "1.0", true);
    private static readonly LanguageInfo DeDe = new("de-DE", "Deutsch", "German", false, "Segoe UI", NumberDigits.Latin, "Eur", "Gregorian", "1.0.0", "1.0", false);

    private sealed record LoggedSut(
        SettingsPageViewModel Sut,
        StubThemeService Theme,
        StubApiEnvironmentService ApiEnvironment,
        StubAuthenticationService Auth,
        StubLanguagePackRepository Packs,
        RecordingLogger<SettingsPageViewModel> Logger);

    private static LoggedSut CreateLoggedSut()
    {
        var theme = new StubThemeService(ThemeMode.Light);
        var apiEnvironment = new StubApiEnvironmentService();
        var auth = new StubAuthenticationService();
        var packs = new StubLanguagePackRepository();
        var logger = new RecordingLogger<SettingsPageViewModel>();
        var sut = new SettingsPageViewModel(new StubLocalizationService([FaIr, EnUs], FaIr), packs, theme, auth, apiEnvironment, logger);
        return new LoggedSut(sut, theme, apiEnvironment, auth, packs, logger);
    }

    private static void AssertSingleErrorFor(RecordingLogger<SettingsPageViewModel> logger, string operation)
    {
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=" + operation, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_SplitsAvailableLanguagesIntoBuiltInAndInstalledPacks()
    {
        var localizationService = new StubLocalizationService([FaIr, EnUs, DeDe], FaIr);

        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService());

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

        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService());

        Assert.Equal("en-US", sut.SelectedLanguage?.Code);
    }

    [Fact]
    public void Constructor_LoadsAvailableLanguagePacksFromRepository()
    {
        var catalog = new[] { new LanguagePackCatalogEntry("fr-FR", "Français", "French", "1.0.0", IsInstalled: false) };
        var localizationService = new StubLocalizationService([FaIr], FaIr);

        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(catalog), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService());

        Assert.Single(sut.AvailableLanguagePacks);
        Assert.Equal("fr-FR", sut.AvailableLanguagePacks[0].Code);
    }

    [Fact]
    public void ApplyLanguageCommand_CanExecute_FalseWhenNoLanguageSelected()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService())
        {
            SelectedLanguage = null,
        };

        Assert.False(sut.ApplyLanguageCommand.CanExecute(null));
    }

    [Fact]
    public void ApplyLanguageCommand_ToDifferentLanguage_PersistsAndSetsRestartRequiredStatus()
    {
        var localizationService = new StubLocalizationService([FaIr, EnUs], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService())
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
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService())
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
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService());
        var entry = new LanguagePackCatalogEntry("fr-FR", "Français", "French", "1.0.0", IsInstalled: false);

        sut.DownloadOrInstallCommand.Execute(entry);

        Assert.False(string.IsNullOrEmpty(sut.StatusMessage));
    }

    [Fact]
    public void RemovePackCommand_RepositoryNotSupported_SurfacesMessageInsteadOfThrowing()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService());

        sut.RemovePackCommand.Execute(DeDe);

        Assert.False(string.IsNullOrEmpty(sut.StatusMessage));
    }

    [Fact]
    public void CurrentLanguageDisplay_IncludesCurrentLanguageNativeName()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), new StubApiEnvironmentService());

        Assert.Contains(FaIr.NativeName, sut.CurrentLanguageDisplay, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_PreselectsCurrentThemeMode()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var themeService = new StubThemeService(ThemeMode.Dark);

        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), themeService, new StubAuthenticationService(), new StubApiEnvironmentService());

        Assert.Equal(ThemeMode.Dark, sut.SelectedThemeMode);
    }

    [Fact]
    public void SelectThemeModeCommand_UpdatesSelectedThemeModeWithoutPersisting()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var themeService = new StubThemeService(ThemeMode.Light);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), themeService, new StubAuthenticationService(), new StubApiEnvironmentService());

        sut.SelectThemeModeCommand.Execute(ThemeMode.Dark);

        Assert.Equal(ThemeMode.Dark, sut.SelectedThemeMode);
        Assert.Null(themeService.LastSetMode);
    }

    [Fact]
    public void ApplyThemeCommand_ToDifferentTheme_PersistsAndSetsRestartRequiredStatus()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var themeService = new StubThemeService(ThemeMode.Light);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), themeService, new StubAuthenticationService(), new StubApiEnvironmentService());
        sut.SelectThemeModeCommand.Execute(ThemeMode.Dark);

        sut.ApplyThemeCommand.Execute(null);

        Assert.Equal("Dark", themeService.LastSetMode);
        Assert.True(sut.IsThemeRestartRequired);
        Assert.Equal(Strings.Settings_Theme_RestartRequired, sut.ThemeStatusMessage);
    }

    [Fact]
    public void ApplyThemeCommand_ToSameTheme_LeavesStatusMessageEmpty()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var themeService = new StubThemeService(ThemeMode.Light);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), themeService, new StubAuthenticationService(), new StubApiEnvironmentService());
        sut.SelectThemeModeCommand.Execute(ThemeMode.Light);

        sut.ApplyThemeCommand.Execute(null);

        Assert.False(sut.IsThemeRestartRequired);
        Assert.Equal(string.Empty, sut.ThemeStatusMessage);
    }

    [Fact]
    public void CurrentThemeDisplay_IncludesLocalizedThemeName()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var themeService = new StubThemeService(ThemeMode.System);
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), themeService, new StubAuthenticationService(), new StubApiEnvironmentService());

        Assert.Contains(Strings.Settings_Theme_System, sut.CurrentThemeDisplay, StringComparison.Ordinal);
    }

    [Fact]
    public void SignOutCommand_CallsTheAuthenticationServicesSignOut()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var authenticationService = new StubAuthenticationService();
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), authenticationService, new StubApiEnvironmentService());

        sut.SignOutCommand.Execute(null);

        Assert.Equal(1, authenticationService.SignOutCallCount);
    }

    [Fact]
    public void ApplyApiEnvironmentCommand_SwitchingToProduction_PersistsUrlAndSetsRestartRequiredStatus()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var apiEnvironmentService = new StubApiEnvironmentService();
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), apiEnvironmentService)
        {
            SelectedApiEnvironment = ApiEnvironment.Production,
            ProductionUrlInput = "https://api.rojan.ai",
        };

        sut.ApplyApiEnvironmentCommand.Execute(null);

        Assert.Equal(ApiEnvironment.Production, apiEnvironmentService.SelectedEnvironment);
        Assert.Equal("https://api.rojan.ai", apiEnvironmentService.ProductionUrl);
        Assert.True(sut.IsApiEnvironmentRestartRequired);
        Assert.Equal(Strings.Settings_ApiEnvironment_RestartRequired, sut.ApiEnvironmentStatusMessage);
    }

    [Fact]
    public void ApplyApiEnvironmentCommand_StayingOnDevelopment_LeavesStatusMessageEmpty()
    {
        var localizationService = new StubLocalizationService([FaIr], FaIr);
        var apiEnvironmentService = new StubApiEnvironmentService();
        var sut = new SettingsPageViewModel(localizationService, new StubLanguagePackRepository(), new StubThemeService(), new StubAuthenticationService(), apiEnvironmentService)
        {
            SelectedApiEnvironment = ApiEnvironment.Development,
        };

        sut.ApplyApiEnvironmentCommand.Execute(null);

        Assert.False(sut.IsApiEnvironmentRestartRequired);
        Assert.Equal(string.Empty, sut.ApiEnvironmentStatusMessage);
    }

    [Fact]
    public void ApplyThemeCommand_Failure_ShowsGenericError_PreservesSelection_LogsOperationOnly()
    {
        var f = CreateLoggedSut();
        f.Sut.SelectThemeModeCommand.Execute(ThemeMode.Dark);
        f.Theme.SetThemeModeException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => f.Sut.ApplyThemeCommand.Execute(null));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, f.Sut.ThemeStatusMessage);
        Assert.Equal(ThemeMode.Dark, f.Sut.SelectedThemeMode);
        AssertSingleErrorFor(f.Logger, "ApplyThemeAsync");
    }

    [Fact]
    public void ApplyThemeCommand_SuccessAfterFailure_ClearsGenericError()
    {
        var f = CreateLoggedSut();
        f.Sut.SelectThemeModeCommand.Execute(ThemeMode.Dark);
        f.Theme.SetThemeModeException = new InvalidOperationException(Secret);
        f.Sut.ApplyThemeCommand.Execute(null);
        Assert.Equal(Strings.Common_ActionFailedMessage, f.Sut.ThemeStatusMessage);

        f.Theme.SetThemeModeException = null;
        f.Sut.ApplyThemeCommand.Execute(null);

        Assert.Equal(Strings.Settings_Theme_RestartRequired, f.Sut.ThemeStatusMessage);
    }

    [Fact]
    public void ApplyApiEnvironmentCommand_Failure_ShowsGenericError_DoesNotLeakUrl_PreservesSelection()
    {
        var f = CreateLoggedSut();
        f.Sut.SelectApiEnvironmentCommand.Execute(ApiEnvironment.Production);
        f.Sut.ProductionUrlInput = Secret;
        f.ApiEnvironment.SetEnvironmentException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => f.Sut.ApplyApiEnvironmentCommand.Execute(null));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, f.Sut.ApiEnvironmentStatusMessage);
        Assert.DoesNotContain(Secret, f.Sut.ApiEnvironmentStatusMessage, StringComparison.Ordinal);
        Assert.Equal(ApiEnvironment.Production, f.Sut.SelectedApiEnvironment);
        Assert.Equal(Secret, f.Sut.ProductionUrlInput);
        AssertSingleErrorFor(f.Logger, "ApplyApiEnvironmentAsync");
    }

    [Fact]
    public void RefreshAvailablePacksCommand_Failure_ShowsGenericError_LogsOperationOnly()
    {
        var f = CreateLoggedSut();
        f.Packs.GetAvailableLanguagePacksException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => f.Sut.RefreshAvailablePacksCommand.Execute(null));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, f.Sut.StatusMessage);
        AssertSingleErrorFor(f.Logger, "RefreshAvailablePacksAsync");
    }

    [Fact]
    public void DownloadOrInstallCommand_UnexpectedFailure_ShowsGenericError_LogsOperationOnly()
    {
        var f = CreateLoggedSut();
        f.Packs.PackMutationException = new InvalidOperationException(Secret);
        var entry = new LanguagePackCatalogEntry("fr-FR", "Français", "French", "1.0.0", IsInstalled: false);

        var exception = Record.Exception(() => f.Sut.DownloadOrInstallCommand.Execute(entry));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, f.Sut.StatusMessage);
        AssertSingleErrorFor(f.Logger, "DownloadOrInstallAsync");
    }

    [Fact]
    public void DownloadOrInstallCommand_NotSupported_KeepsStaticMessage_DoesNotLog()
    {
        var f = CreateLoggedSut();
        var entry = new LanguagePackCatalogEntry("fr-FR", "Français", "French", "1.0.0", IsInstalled: false);

        f.Sut.DownloadOrInstallCommand.Execute(entry);

        Assert.False(string.IsNullOrEmpty(f.Sut.StatusMessage));
        Assert.NotEqual(Strings.Common_ActionFailedMessage, f.Sut.StatusMessage);
        Assert.Empty(f.Logger.Entries);
    }

    [Fact]
    public void RemovePackCommand_UnexpectedFailure_ShowsGenericError_LogsOperationOnly()
    {
        var f = CreateLoggedSut();
        f.Packs.PackMutationException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => f.Sut.RemovePackCommand.Execute(DeDe));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, f.Sut.StatusMessage);
        AssertSingleErrorFor(f.Logger, "RemovePackAsync");
    }

    [Fact]
    public void SignOutCommand_Failure_ShowsAccountError_LeavesAuthStateConsistent_LogsOperationOnly()
    {
        var f = CreateLoggedSut();
        f.Auth.SignOutException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => f.Sut.SignOutCommand.Execute(null));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, f.Sut.AccountStatusMessage);
        Assert.Equal(1, f.Auth.SignOutCallCount);
        Assert.Equal(Rojan.Desktop.Domain.Security.AuthenticationState.Authenticated, f.Auth.CurrentState);
        AssertSingleErrorFor(f.Logger, "SignOutAsync");
    }

    [Fact]
    public void SignOutCommand_Success_LeavesAccountStatusEmpty()
    {
        var f = CreateLoggedSut();

        f.Sut.SignOutCommand.Execute(null);

        Assert.Equal(string.Empty, f.Sut.AccountStatusMessage);
        Assert.Equal(Rojan.Desktop.Domain.Security.AuthenticationState.SignedOut, f.Auth.CurrentState);
        Assert.Empty(f.Logger.Entries);
    }
}
