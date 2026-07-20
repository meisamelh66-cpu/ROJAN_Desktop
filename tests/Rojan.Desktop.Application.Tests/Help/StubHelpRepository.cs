using Rojan.Desktop.Domain.Help;

namespace Rojan.Desktop.Application.Tests.Help;

/// <summary>Fixed-list <see cref="IHelpRepository"/> test double - lets a test control exactly which topics (and their versions) exist.</summary>
internal sealed class StubHelpRepository : IHelpRepository
{
    private readonly IReadOnlyList<HelpTopic> _topics;

    public StubHelpRepository(IReadOnlyList<HelpTopic> topics)
    {
        _topics = topics;
    }

    public Task<IReadOnlyList<HelpTopic>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_topics);

    public Task<HelpTopic?> GetByIdAsync(string topicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_topics.FirstOrDefault(topic => topic.Id == topicId));
}
