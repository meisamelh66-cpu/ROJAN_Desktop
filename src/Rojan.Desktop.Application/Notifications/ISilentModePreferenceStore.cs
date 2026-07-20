namespace Rojan.Desktop.Application.Notifications;

/// <summary>Phase 27's Silent Mode architecture - persisted user preference, implemented by <c>Infrastructure.Notifications.LocalSilentModePreferenceStore</c> so the setting survives an app restart, the same persisted-preference shape <c>Application.Help.IHelpFavoritesStore</c> already established.</summary>
public interface ISilentModePreferenceStore
{
    public Task<bool> GetIsEnabledAsync(CancellationToken cancellationToken = default);

    public Task SetIsEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default);
}
