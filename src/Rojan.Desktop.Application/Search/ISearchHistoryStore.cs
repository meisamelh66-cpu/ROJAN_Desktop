namespace Rojan.Desktop.Application.Search;

/// <summary>Phase 28's Recent Searches requirement - persisted query history, implemented by <c>Infrastructure.Search.LocalSearchHistoryStore</c> so it survives an app restart, the same persisted-history shape <c>Application.Help.IHelpRecentlyViewedStore</c> already established.</summary>
public interface ISearchHistoryStore
{
    /// <summary>Most-recently-searched first, capped at the store's own retention limit.</summary>
    public Task<IReadOnlyList<string>> GetRecentSearchesAsync(CancellationToken cancellationToken = default);

    /// <summary>Records <paramref name="query"/> as just-searched, moving it to the front if already present.</summary>
    public Task RecordSearchAsync(string query, CancellationToken cancellationToken = default);

    public Task ClearAsync(CancellationToken cancellationToken = default);
}
