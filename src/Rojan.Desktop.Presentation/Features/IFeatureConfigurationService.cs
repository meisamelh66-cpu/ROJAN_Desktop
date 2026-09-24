namespace Rojan.Desktop.Presentation.Features;

/// <summary>
/// PASS D10 (Feature Personalization &amp; Modular Workspace): which
/// <c>Modules.FeatureConfigurability.Optional</c> module ids the current salon has chosen to keep
/// visible - a personalization/workspace-configuration concern, never a permission system (a hidden
/// feature is still fully installed, its data untouched; the account may well have real backend
/// permission for it). Same "interface in Presentation, concrete implementation in Shell" split as
/// <see cref="Organizations.ICurrentSessionService"/>/<c>Theming.IThemeService</c> - the real
/// implementation needs file-system access (persisted per-salon selection) that only the composition
/// root should own.
///
/// Scope: per Windows user (the settings file already lives under the current user's
/// <c>%LocalAppData%</c>, same as every other local preference in this app) *and* per salon (keyed
/// inside that one file by the resolved salon id) - switching which salon is active must never let
/// one salon's chosen feature set silently overwrite another's. A session with no resolved salon yet
/// (demo mode, or a brand-new invitee with <c>DesktopContextState.NoBusinessContext</c>) falls back to
/// one fixed, shared bucket - there is no real salon identity to key by yet.
///
/// Unlike <see cref="Organizations.ICurrentSessionService"/>'s <c>CurrentOrganization</c>/<c>CurrentRole</c>
/// (which change live via <c>SwitchBranchAsync</c>/<c>SwitchRoleAsync</c>), the salon this service scopes
/// to is fixed for the lifetime of one running session - real sessions cannot switch to a different real
/// salon without a fresh app launch (see <c>Shell.Organizations.CurrentSessionService.SwitchBranchAsync</c>'s
/// own guard) - so <see cref="InitializeAsync"/> resolves the scope once, not on every session change.
/// </summary>
public interface IFeatureConfigurationService
{
    /// <summary>
    /// True once an explicit configuration has been found (via <see cref="InitializeAsync"/>) or saved
    /// (via <see cref="SaveEnabledFeatureKeysAsync"/>) for the current salon - an empty saved selection
    /// still counts as "configured" (the user deliberately chose to keep nothing extra visible), so this
    /// is never conflated with <see cref="EnabledFeatureKeys"/> being empty. False is the one signal that
    /// should trigger first-run workspace setup.
    /// </summary>
    public bool HasConfiguration { get; }

    /// <summary>
    /// The saved <c>Modules.FeatureConfigurability.Optional</c> module ids currently enabled for the
    /// current salon - cached in memory after <see cref="InitializeAsync"/>/<see cref="SaveEnabledFeatureKeysAsync"/>,
    /// so callers on the sidebar's hot rendering path (<c>MainWindowViewModel.BuildVisibleNavigationItems</c>)
    /// never need to await file I/O. A module id absent from this set (including one that did not exist
    /// yet the last time the user saved) is simply hidden - see <see cref="IsFeatureEnabled"/>'s own doc
    /// comment for why that is the correct, deterministic behavior for a brand-new future feature.
    /// </summary>
    public IReadOnlySet<string> EnabledFeatureKeys { get; }

    /// <summary>
    /// Raised after <see cref="SaveEnabledFeatureKeysAsync"/> completes - lets
    /// <c>MainWindowViewModel</c> refresh the visible sidebar live, the same "no restart required"
    /// shape <c>ICurrentSessionService.SessionChanged</c> already establishes for a branch/role switch.
    /// </summary>
    public event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Loads the persisted configuration scoped to the current salon (see this interface's own doc
    /// comment for what "current salon" means and how the scope is resolved) - <c>Shell.App.OnStartup</c>
    /// is the one real caller, after <c>ICurrentSessionService.InitializeAsync</c> has resolved the
    /// active salon and before <c>MainWindow</c>/<c>MainWindowViewModel</c> are constructed, so the very
    /// first sidebar build already reflects the real saved configuration (or triggers first-run setup).
    /// A missing, corrupt, or unreadable settings file is treated exactly like "never configured" -
    /// <see cref="HasConfiguration"/> is false, never a startup failure.
    /// </summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>True if <paramref name="featureKey"/> is in <see cref="EnabledFeatureKeys"/> - callers combine this with a module's own mandatory/not-configurable status themselves (this method only ever answers "did the user turn this optional feature on").</summary>
    public bool IsFeatureEnabled(string featureKey);

    /// <summary>
    /// Persists a complete snapshot of enabled configurable-feature keys for the current salon,
    /// updates <see cref="EnabledFeatureKeys"/>/<see cref="HasConfiguration"/> immediately, and raises
    /// <see cref="ConfigurationChanged"/>. Always the full current set from the caller (first-run setup
    /// or the Settings personalization screen, both of which always display every real configurable
    /// feature) - never a delta - so a module id genuinely absent from every past save reliably means
    /// "the user has never turned this on," not an ambiguous partial write.
    /// </summary>
    public Task SaveEnabledFeatureKeysAsync(IReadOnlySet<string> enabledFeatureKeys, CancellationToken cancellationToken = default);
}
