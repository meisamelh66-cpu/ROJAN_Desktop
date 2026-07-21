using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Presentation.Tests.Search;

/// <summary>In-memory <see cref="ISearchFavoritesStore"/> test double.</summary>
internal sealed class StubSearchFavoritesStore : ISearchFavoritesStore
{
    private readonly HashSet<string> _favorites = [];

    public Task<IReadOnlySet<string>> GetFavoriteIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(_favorites);

    public Task<bool> ToggleFavoriteAsync(string candidateId, CancellationToken cancellationToken = default)
    {
        var isNowFavorite = _favorites.Remove(candidateId) is false;
        if (isNowFavorite)
        {
            _favorites.Add(candidateId);
        }

        return Task.FromResult(isNowFavorite);
    }
}
