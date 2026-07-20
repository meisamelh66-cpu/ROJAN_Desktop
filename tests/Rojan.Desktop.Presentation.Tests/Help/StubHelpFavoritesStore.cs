using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Presentation.Tests.Help;

/// <summary>In-memory <see cref="IHelpFavoritesStore"/> test double.</summary>
internal sealed class StubHelpFavoritesStore : IHelpFavoritesStore
{
    private readonly HashSet<string> _favorites = [];

    public Task<IReadOnlySet<string>> GetFavoriteTopicIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(_favorites);

    public Task<bool> ToggleFavoriteAsync(string topicId, CancellationToken cancellationToken = default)
    {
        var isNowFavorite = _favorites.Remove(topicId) is false;
        if (isNowFavorite)
        {
            _favorites.Add(topicId);
        }

        return Task.FromResult(isNowFavorite);
    }
}
