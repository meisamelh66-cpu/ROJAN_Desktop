using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Presentation.Tests.Search;

/// <summary>In-memory <see cref="ISearchHistoryStore"/> test double.</summary>
internal sealed class StubSearchHistoryStore : ISearchHistoryStore
{
    private readonly List<string> _recent = [];

    public Task<IReadOnlyList<string>> GetRecentSearchesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(_recent);

    public Task RecordSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        _recent.Insert(0, query);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _recent.Clear();
        return Task.CompletedTask;
    }
}
