namespace Rojan.Desktop.Application.Help;

/// <summary>Application-layer shape of a help topic, mapped from <see cref="Rojan.Desktop.Domain.Help.HelpTopic"/> by <see cref="HelpQueryService"/> - so nothing Domain-shaped crosses into Presentation, the same reasoning every other module's Dto/QueryService pair already follows.</summary>
public sealed record HelpTopicDto(
    string Id,
    string ModuleId,
    string? PageId,
    string KeyPrefix,
    IReadOnlyList<HelpShortcutDto> Shortcuts,
    IReadOnlyList<string> RelatedTopicIds);
