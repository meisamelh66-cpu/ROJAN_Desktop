namespace Rojan.Desktop.Application.Help;

/// <summary>Phase 26: Smart Context Help Engine. Help content resolution (Phase 26.1) - every topic, one topic by id, or the best-matching topic for a given module/page context.</summary>
public interface IHelpQueryService
{
    public Task<IReadOnlyList<HelpTopicDto>> GetAllTopicsAsync(CancellationToken cancellationToken = default);

    public Task<HelpTopicDto?> GetTopicByIdAsync(string topicId, CancellationToken cancellationToken = default);

    /// <summary>Resolves the best topic for the given context (Phase 26.1's "context/page/module detection" responsibility) - an exact module+page match, then a module-level topic, then the well-known default topic (<see cref="HelpQueryService.DefaultTopicId"/>) if even that fails to resolve or was itself the match. Never returns <see langword="null"/> unless the default topic is missing entirely from the registry.</summary>
    public Task<HelpTopicDto?> GetTopicForContextAsync(string moduleId, string? pageId = null, CancellationToken cancellationToken = default);
}
