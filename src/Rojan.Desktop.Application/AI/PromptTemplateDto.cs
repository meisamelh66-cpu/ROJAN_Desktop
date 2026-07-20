namespace Rojan.Desktop.Application.AI;

public sealed record PromptTemplateDto(
    string Id,
    string Name,
    InsightCategory Category,
    string Body,
    bool IsSystemDefined);
