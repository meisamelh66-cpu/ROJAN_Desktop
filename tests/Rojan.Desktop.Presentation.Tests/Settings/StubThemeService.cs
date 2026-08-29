using Rojan.Desktop.Presentation.Theming;

namespace Rojan.Desktop.Presentation.Tests.Settings;

/// <summary>Fakes <see cref="IThemeService"/> for SettingsPageViewModel tests - no file-system/registry access, unlike the real Shell.Theming.ThemeService.</summary>
internal sealed class StubThemeService : IThemeService
{
    private readonly ThemeMode _resolvedForSystem;

    public StubThemeService(ThemeMode selectedMode = ThemeMode.Light, ThemeMode resolvedForSystem = ThemeMode.Light)
    {
        SelectedMode = selectedMode;
        _resolvedForSystem = resolvedForSystem;
        ResolvedTheme = selectedMode == ThemeMode.System ? resolvedForSystem : selectedMode;
    }

    public ThemeMode SelectedMode { get; private set; }

    public ThemeMode ResolvedTheme { get; private set; }

    public bool IsRestartRequired { get; private set; }

    public string? LastSetMode { get; private set; }

    /// <summary>Optional failure hook - when set, <see cref="SetThemeModeAsync"/> faults with this exception (call not recorded).</summary>
    public Exception? SetThemeModeException { get; set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SetThemeModeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        if (SetThemeModeException is not null)
        {
            return Task.FromException(SetThemeModeException);
        }

        LastSetMode = mode.ToString();

        var newlyResolved = mode == ThemeMode.System ? _resolvedForSystem : mode;
        if (newlyResolved != ResolvedTheme)
        {
            IsRestartRequired = true;
        }

        SelectedMode = mode;
        return Task.CompletedTask;
    }
}
