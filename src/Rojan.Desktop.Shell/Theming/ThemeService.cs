using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using Rojan.Desktop.Presentation.Theming;

namespace Rojan.Desktop.Shell.Theming;

/// <summary>
/// Default <see cref="IThemeService"/> implementation. Persists the
/// selected <see cref="ThemeMode"/> to a small JSON file under the
/// user's LocalAppData folder (own file, separate from
/// <c>Localization.LocalizationSettingsFile</c>'s settings.json - same
/// "one concern, one file" shape) and reads it back on
/// <see cref="InitializeAsync"/>, called once by <c>App.OnStartup</c>
/// before the design-system resource tree is assembled - so
/// <see cref="ResolvedTheme"/> is correct before any Window/Style is
/// created. Light is always the first-launch default (no settings file
/// yet -> Light), per this pass's "Default should be Light" requirement.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const ThemeMode DefaultMode = ThemeMode.Light;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _settingsFilePath;

    public ThemeService()
        : this(DefaultSettingsFilePath())
    {
    }

    internal ThemeService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public ThemeMode SelectedMode { get; private set; } = DefaultMode;

    public ThemeMode ResolvedTheme { get; private set; } = ThemeMode.Light;

    public bool IsRestartRequired { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        SelectedMode = ReadPersistedMode() ?? DefaultMode;
        ResolvedTheme = Resolve(SelectedMode);
        return Task.CompletedTask;
    }

    public Task SetThemeModeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        PersistMode(mode);

        var newlyResolved = Resolve(mode);
        if (newlyResolved != ResolvedTheme)
        {
            IsRestartRequired = true;
        }

        SelectedMode = mode;
        return Task.CompletedTask;
    }

    /// <summary>Resolves <see cref="ThemeMode.System"/> against the live Windows "AppsUseLightTheme" registry setting; <see cref="ThemeMode.Light"/>/<see cref="ThemeMode.Dark"/> resolve to themselves.</summary>
    private static ThemeMode Resolve(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => ThemeMode.Light,
        ThemeMode.Dark => ThemeMode.Dark,
        ThemeMode.System => ResolveSystemTheme(),
        _ => DefaultMode,
    };

    private static ThemeMode ResolveSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int useLightTheme)
            {
                return useLightTheme == 0 ? ThemeMode.Dark : ThemeMode.Light;
            }
        }
#pragma warning disable CA1031 // A missing/unreadable registry key must fall back to the default theme, not crash startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Fall through to the default below.
        }

        return DefaultMode;
    }

    private ThemeMode? ReadPersistedMode()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<ThemeSettingsFile>(json, SerializerOptions);
            return Enum.TryParse<ThemeMode>(settings?.Mode, ignoreCase: true, out var mode) ? mode : null;
        }
#pragma warning disable CA1031 // A corrupt settings file must fall back to the default theme, not crash startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private void PersistMode(ThemeMode mode)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new ThemeSettingsFile { Mode = mode.ToString() }, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private static string DefaultSettingsFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "theme.json");
}
