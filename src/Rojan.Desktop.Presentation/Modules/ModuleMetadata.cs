using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// PASS D10 (Feature Personalization &amp; Modular Workspace): a module's participation in the
/// optional-feature/workspace-personalization system - deliberately three states, not a bool, because
/// "not configurable" and "mandatory" are both "always visible" but for different reasons that matter
/// to <c>FeatureSetupWindowViewModel</c>/<c>SettingsPageViewModel</c>'s toggle list (mandatory belongs
/// on it, fixed/checked; not-configurable must never appear on it at all - e.g. <c>AcceptInviteModule</c>,
/// whose visibility is already entirely session-state-driven and must stay that way, untouched by this
/// system).
/// </summary>
public enum FeatureConfigurability
{
    /// <summary>Default - this module is not part of the feature-personalization system at all. Its existing visibility rule (unconditional, or permission-gated) is completely unaffected by <c>IFeatureConfigurationService</c>.</summary>
    NotConfigurable = 0,

    /// <summary>Core infrastructure - always visible, never filtered by feature configuration, never appears as a togglable row (shown fixed/checked for transparency only).</summary>
    Mandatory,

    /// <summary>A real optional capability - visible only when <c>IFeatureConfigurationService.IsFeatureEnabled</c> says so for <see cref="ModuleMetadata.Id"/>, and appears as a togglable row in the first-run setup and Settings personalization screens.</summary>
    Optional,
}

/// <summary>
/// Pure display data for a module - everything the sidebar needs to show
/// an entry, with no knowledge of how that module actually activates.
/// Kept separate from <see cref="ModuleDescriptor"/> so navigation UI can
/// depend on metadata alone without pulling in activation concerns.
///
/// Phase 22: <see cref="RequiredPermission"/> is an additive, optional
/// trailing parameter (defaults to <see langword="null"/>) - every module
/// registered before this phase keeps constructing
/// <c>ModuleMetadata(id, title, icon, order)</c> unchanged and stays
/// unconditionally visible (no permission required); only a module that
/// explicitly opts in by passing one gets filtered by
/// <c>MainWindowViewModel</c>'s permission-aware navigation. This is the
/// entire mechanism - no existing module's registration needed to change.
///
/// PASS D10: <see cref="FeatureConfigurability"/>/<see cref="DefaultFeatureEnabled"/> are the same
/// kind of additive, optional trailing parameters - every module that doesn't pass them defaults to
/// <see cref="Modules.FeatureConfigurability.NotConfigurable"/> and stays completely unaffected by
/// the feature-personalization system, same "opt-in, non-breaking" shape Phase 22 already
/// established for <see cref="RequiredPermission"/>. This <see cref="ModuleMetadata"/> record is
/// deliberately reused as-is rather than introducing a parallel "FeatureDefinition" type - it already
/// carries everything a configurable-feature entry needs (stable <see cref="Id"/>, localized
/// <see cref="Title"/>, <see cref="IconGlyph"/>, and now its feature-configurability/default), and
/// <see cref="IModuleRegistry"/> is already the authoritative registry <c>FeatureSetupWindowViewModel</c>/
/// <c>SettingsPageViewModel</c> read from - a second registry would only risk the two disagreeing.
/// </summary>
public sealed record ModuleMetadata(
    string Id,
    string Title,
    string IconGlyph,
    int Order,
    Permission? RequiredPermission = null,
    FeatureConfigurability FeatureConfigurability = FeatureConfigurability.NotConfigurable,
    bool DefaultFeatureEnabled = true);
