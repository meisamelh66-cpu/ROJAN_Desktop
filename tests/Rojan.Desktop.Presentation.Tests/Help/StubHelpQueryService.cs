using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Presentation.Tests.Help;

/// <summary>Fixed-list <see cref="IHelpQueryService"/> test double - lets a ViewModel test control exactly which topics exist and their module/page/related-topic wiring, without a real registry.</summary>
internal sealed class StubHelpQueryService : IHelpQueryService
{
    private readonly List<HelpTopicDto> _topics;

    public StubHelpQueryService(IEnumerable<HelpTopicDto> topics)
    {
        _topics = topics.ToList();
    }

    public Task<IReadOnlyList<HelpTopicDto>> GetAllTopicsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HelpTopicDto>>(_topics);

    public Task<HelpTopicDto?> GetTopicByIdAsync(string topicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_topics.FirstOrDefault(topic => topic.Id == topicId));

    public Task<HelpTopicDto?> GetTopicForContextAsync(string moduleId, string? pageId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_topics.FirstOrDefault(topic => topic.ModuleId == moduleId));
}
