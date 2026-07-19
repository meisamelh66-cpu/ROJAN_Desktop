using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Tests.Settings;

/// <summary>Fakes <see cref="ILocalizationService"/> for SettingsPageViewModel tests - no file-system/process access, unlike the real Shell.Localization.LocalizationService.</summary>
internal sealed class StubLocalizationService : ILocalizationService
{
    private readonly List<LanguageInfo> _availableLanguages;

    public StubLocalizationService(IReadOnlyList<LanguageInfo> availableLanguages, LanguageInfo currentLanguage)
    {
        _availableLanguages = availableLanguages.ToList();
        CurrentLanguage = currentLanguage;
    }

    public LanguageInfo CurrentLanguage { get; private set; }

    public IReadOnlyList<LanguageInfo> AvailableLanguages => _availableLanguages;

    public bool IsRestartRequired { get; private set; }

    public string? LastSetLanguageCode { get; private set; }

    public bool ThrowOnSetLanguage { get; set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSetLanguage)
        {
            throw new InvalidOperationException($"Language '{languageCode}' is not available.");
        }

        LastSetLanguageCode = languageCode;
        if (!string.Equals(languageCode, CurrentLanguage.Code, StringComparison.Ordinal))
        {
            IsRestartRequired = true;
        }

        return Task.CompletedTask;
    }
}
