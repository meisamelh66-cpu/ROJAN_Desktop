namespace Rojan.Desktop.Presentation.Theming;

/// <summary>
/// The Fluent 2 Premium Theme pass's theme façade - same "interface in
/// Presentation, concrete implementation in Shell" split as
/// <c>Navigation.INavigationService</c>/<c>Localization.ILocalizationService</c>,
/// since the real implementation needs file-system access (persisted
/// selection) and Windows-registry access (resolving <see cref="ThemeMode.System"/>),
/// both Shell-level concerns. Theme changes take effect on next launch
/// ("restart required" - see <see cref="IsRestartRequired"/>), not live -
/// the whole app-level design-system resource tree
/// (<c>Shell.Theming.ThemeResources.Build</c>) is assembled once at
/// startup from <see cref="ResolvedTheme"/>, mirroring exactly how
/// <c>ILocalizationService</c>'s culture/RTL flip already works.
/// </summary>
public interface IThemeService
{
    /// <summary>The user's persisted preference - Light, Dark, or System. Defaults to <see cref="ThemeMode.Light"/> on first launch (no settings file yet), per this pass's "Default should be Light" requirement.</summary>
    public ThemeMode SelectedMode { get; }

    /// <summary>What's actually active this session - always <see cref="ThemeMode.Light"/> or <see cref="ThemeMode.Dark"/>, never <see cref="ThemeMode.System"/> (already resolved against the Windows theme setting during <see cref="InitializeAsync"/>).</summary>
    public ThemeMode ResolvedTheme { get; }

    /// <summary>True once <see cref="SetThemeModeAsync"/> has selected a mode whose resolved theme differs from <see cref="ResolvedTheme"/> - the UI should prompt for a restart, same UX as <c>ILocalizationService.IsRestartRequired</c>.</summary>
    public bool IsRestartRequired { get; }

    /// <summary>Loads the persisted theme selection (defaulting to Light if none is saved yet) and resolves <see cref="ResolvedTheme"/> - called once at startup, before any Window or design-system resource is created.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists <paramref name="mode"/> as the mode to resolve on next launch and sets <see cref="IsRestartRequired"/> if its resolved theme differs from the currently-running session's. Does not change the running session's theme.</summary>
    public Task SetThemeModeAsync(ThemeMode mode, CancellationToken cancellationToken = default);
}
