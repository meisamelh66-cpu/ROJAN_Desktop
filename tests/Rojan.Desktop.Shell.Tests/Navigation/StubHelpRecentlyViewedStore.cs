using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>In-memory <see cref="IHelpRecentlyViewedStore"/> test double - avoids touching the real %LocalAppData% JSON file <c>LocalHelpRecentlyViewedStore</c> persists to.</summary>
internal sealed class StubHelpRecentlyViewedStore : IHelpRecentlyViewedStore
{
    private readonly List<string> _recent = [];

    public Task<IReadOnlyList<string>> GetRecentTopicIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(_recent);

    public Task RecordViewedAsync(string topicId, CancellationToken cancellationToken = default)
    {
        _recent.Remove(topicId);
        _recent.Insert(0, topicId);
        return Task.CompletedTask;
    }
}
