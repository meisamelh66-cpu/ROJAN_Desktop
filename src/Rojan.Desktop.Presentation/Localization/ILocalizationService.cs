namespace Rojan.Desktop.Presentation.Localization;

/// <summary>
/// The main façade every ViewModel/View depends on for "what language am
/// I in, and how do I change it" - same "interface in Presentation,
/// concrete implementation in Shell" split as
/// <c>Navigation.INavigationService</c>/<c>Dialogs.IDialogService</c>,
/// since the real implementation needs file-system access (persisted
/// selection) and owns app-startup sequencing, both Shell-level concerns.
/// Language changes take effect on next launch ("restart required" - see
/// <see cref="IsRestartRequired"/>), not live - so nothing here needs to
/// be an <c>INotifyPropertyChanged</c> reactive property; XAML strings
/// resolve once at startup via <c>{x:Static loc:Strings.Key}</c> and stay
/// correct for the rest of the session.
/// </summary>
public interface ILocalizationService
{
    /// <summary>The language active for this running session - Persian on first launch, per Phase 19A's default application state.</summary>
    public LanguageInfo CurrentLanguage { get; }

    /// <summary>Built-in languages plus every discovered installed pack (see <see cref="ILanguagePackManager"/>).</summary>
    public IReadOnlyList<LanguageInfo> AvailableLanguages { get; }

    /// <summary>True once <see cref="SetLanguageAsync"/> has selected a language different from <see cref="CurrentLanguage"/> - the UI should prompt for a restart, per Phase 19A's persistence design.</summary>
    public bool IsRestartRequired { get; }

    /// <summary>Loads the persisted language selection (defaulting to Persian if none is saved yet) and discovers every available language via <see cref="ILanguagePackManager"/> - called once at startup, before any Window is created.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists <paramref name="languageCode"/> as the language to load on next launch and sets <see cref="IsRestartRequired"/> if it differs from <see cref="CurrentLanguage"/>. Does not change the running session's language.</summary>
    public Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default);
}
