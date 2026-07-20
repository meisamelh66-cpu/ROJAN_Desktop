using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Presentation.Tests.Help;

/// <summary>In-memory <see cref="IHelpRecentlyViewedStore"/> test double.</summary>
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
