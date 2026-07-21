namespace Rojan.Desktop.Application.Search;

/// <summary>Phase 28's Favorites requirement - persisted favorite result ids (a <see cref="SearchCandidate.Id"/>), implemented by <c>Infrastructure.Search.LocalSearchFavoritesStore</c>, the same persisted-favorites shape <c>Application.Help.IHelpFavoritesStore</c> already established.</summary>
public interface ISearchFavoritesStore
{
    public Task<IReadOnlySet<string>> GetFavoriteIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds <paramref name="candidateId"/> if not already favorited, removes it otherwise. Returns the new state (<see langword="true"/> if now favorited).</summary>
    public Task<bool> ToggleFavoriteAsync(string candidateId, CancellationToken cancellationToken = default);
}
