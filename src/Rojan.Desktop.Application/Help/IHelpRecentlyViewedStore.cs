namespace Rojan.Desktop.Application.Help;

/// <summary>Phase 26.7/26.12: Help Navigation's "Recently Viewed" plus the "cache recently used topics" performance requirement - one store satisfies both, since a recently-viewed topic id list is exactly a recency cache's eviction policy already needs.</summary>
public interface IHelpRecentlyViewedStore
{
    /// <summary>Most-recently-viewed first, capped at the store's own retention limit.</summary>
    public Task<IReadOnlyList<string>> GetRecentTopicIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Records <paramref name="topicId"/> as just-viewed, moving it to the front if already present.</summary>
    public Task RecordViewedAsync(string topicId, CancellationToken cancellationToken = default);
}
