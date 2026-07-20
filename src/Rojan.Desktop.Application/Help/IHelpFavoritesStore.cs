namespace Rojan.Desktop.Application.Help;

/// <summary>Phase 26.7: Help Navigation - "Favorites (architecture only)". A real, working store (not a stub) so the extension point genuinely functions end-to-end, even though this phase does not build a dedicated favorites-browsing UI beyond a toggle in the dialog.</summary>
public interface IHelpFavoritesStore
{
    public Task<IReadOnlySet<string>> GetFavoriteTopicIdsAsync(CancellationToken cancellationToken = default);

    public Task<bool> ToggleFavoriteAsync(string topicId, CancellationToken cancellationToken = default);
}
