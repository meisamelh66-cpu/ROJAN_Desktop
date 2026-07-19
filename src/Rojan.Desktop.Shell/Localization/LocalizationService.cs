using System.IO;
using System.Text.Json;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Shell.Localization;

/// <summary>
/// Default <see cref="ILocalizationService"/> implementation. Persists the
/// selected language to a small JSON file under the user's LocalAppData
/// folder (this app's established "no database yet" convention extends
/// to settings too) and reads it back on <see cref="InitializeAsync"/>,
/// called once by <c>App.OnStartup</c> before the Generic Host/Window are
/// built - so <see cref="CurrentLanguage"/>, the process's
/// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>, and
/// every <c>{x:Static loc:Strings.Key}</c> XAML binding are all correct
/// from the very first frame rendered. Persian is always the first-launch
/// default (no settings file yet -> "fa-IR").
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private const string DefaultLanguageCode = "fa-IR";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly ILanguagePackManager _packManager;
    private readonly string _settingsFilePath;
    private List<LanguageInfo> _availableLanguages = [];

    public LocalizationService(ILanguagePackManager packManager)
        : this(packManager, DefaultSettingsFilePath())
    {
    }

    internal LocalizationService(ILanguagePackManager packManager, string settingsFilePath)
    {
        _packManager = packManager;
        _settingsFilePath = settingsFilePath;
    }

    public LanguageInfo CurrentLanguage { get; private set; } = null!;

    public IReadOnlyList<LanguageInfo> AvailableLanguages => _availableLanguages;

    public bool IsRestartRequired { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _availableLanguages = (await _packManager.DiscoverLanguagesAsync(cancellationToken).ConfigureAwait(false)).ToList();
        if (_availableLanguages.Count == 0)
        {
            // Defensive fallback: even if the Languages/ folder is somehow
            // missing or empty, the app must still start in Persian - see
            // Phase 19A's "Default Application State" requirement.
            _availableLanguages.Add(new LanguageInfo(
                DefaultLanguageCode, "فارسی", "Persian", true, "Vazirmatn, Segoe UI",
                NumberDigits.Persian, "Toman", "Persian", "1.0.0", "1.0", true));
        }

        var savedCode = ReadPersistedLanguageCode();
        CurrentLanguage =
            _availableLanguages.FirstOrDefault(language => language.Code == savedCode) ??
            _availableLanguages.FirstOrDefault(language => language.Code == DefaultLanguageCode) ??
            _availableLanguages[0];

        var overrides = await _packManager.GetPackStringOverridesAsync(CurrentLanguage.Code, cancellationToken).ConfigureAwait(false);
        Strings.SetPackOverrides(overrides);
    }

    public Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        var target = _availableLanguages.FirstOrDefault(language => language.Code == languageCode)
            ?? throw new InvalidOperationException($"Language '{languageCode}' is not available.");

        PersistLanguageCode(target.Code);

        if (!string.Equals(target.Code, CurrentLanguage.Code, StringComparison.Ordinal))
        {
            IsRestartRequired = true;
        }

        return Task.CompletedTask;
    }

    private string? ReadPersistedLanguageCode()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<LocalizationSettingsFile>(json, SerializerOptions);
            return string.IsNullOrWhiteSpace(settings?.Language) ? null : settings.Language;
        }
#pragma warning disable CA1031 // A corrupt settings file must fall back to the default language, not crash startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private void PersistLanguageCode(string code)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new LocalizationSettingsFile { Language = code }, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private static string DefaultSettingsFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "settings.json");
}
