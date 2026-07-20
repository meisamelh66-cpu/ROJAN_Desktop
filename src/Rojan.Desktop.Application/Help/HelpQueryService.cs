using Rojan.Desktop.Domain.Help;

namespace Rojan.Desktop.Application.Help;

/// <summary>
/// Default <see cref="IHelpQueryService"/>. Fetches every topic from
/// <see cref="IHelpRepository"/>, filters out anything
/// <see cref="HelpContentRules.IsVersionCompatible"/> rejects (Phase
/// 26.1's "version compatibility"), then delegates the actual matching
/// to <see cref="HelpContentRules.ResolveContext"/> (Application is
/// allowed to depend on Domain) before falling back to
/// <see cref="DefaultTopicId"/> - a generic "what is this screen"
/// topic every registry is expected to seed, so a page with no
/// dedicated content yet still gets a non-empty, useful help dialog
/// instead of an error state.
/// </summary>
public sealed class HelpQueryService : IHelpQueryService
{
    /// <summary>The generic fallback topic id (<c>Infrastructure.Help.HelpTopicRegistry</c> seeds one under this id) shown when no module/page-specific topic exists yet.</summary>
    public const string DefaultTopicId = "help-default";

    /// <summary>Kept in sync with <c>Directory.Build.props</c>' <c>VersionPrefix</c> - Phase 26 has no runtime access to the assembly's own informational version from Application (that is an Infrastructure/Shell concern), and a hardcoded major-version literal here is honest: this is the version help-content authors write against, not a value that needs to track every patch release.</summary>
    public const string CurrentAppVersion = "1.0.0";

    private readonly IHelpRepository _repository;

    public HelpQueryService(IHelpRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<HelpTopicDto>> GetAllTopicsAsync(CancellationToken cancellationToken = default)
    {
        var topics = await CompatibleTopicsAsync(cancellationToken).ConfigureAwait(false);
        return topics.Select(Map).ToList();
    }

    public async Task<HelpTopicDto?> GetTopicByIdAsync(string topicId, CancellationToken cancellationToken = default)
    {
        var topic = await _repository.GetByIdAsync(topicId, cancellationToken).ConfigureAwait(false);
        return topic is null || !HelpContentRules.IsVersionCompatible(topic.Version, CurrentAppVersion) ? null : Map(topic);
    }

    public async Task<HelpTopicDto?> GetTopicForContextAsync(string moduleId, string? pageId = null, CancellationToken cancellationToken = default)
    {
        var topics = await CompatibleTopicsAsync(cancellationToken).ConfigureAwait(false);

        var resolved = HelpContentRules.ResolveContext(topics, moduleId, pageId);
        if (resolved is not null)
        {
            return Map(resolved);
        }

        var fallback = topics.FirstOrDefault(topic => topic.Id == DefaultTopicId);
        return fallback is null ? null : Map(fallback);
    }

    private async Task<IReadOnlyList<HelpTopic>> CompatibleTopicsAsync(CancellationToken cancellationToken)
    {
        var topics = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return topics.Where(topic => HelpContentRules.IsVersionCompatible(topic.Version, CurrentAppVersion)).ToList();
    }

    private static HelpTopicDto Map(HelpTopic topic) => new(
        topic.Id,
        topic.ModuleId,
        topic.PageId,
        topic.KeyPrefix,
        topic.Shortcuts.Select(s => new HelpShortcutDto(s.KeysDisplay, s.DescriptionKey)).ToList(),
        topic.RelatedTopicIds);
}
