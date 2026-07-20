namespace Rojan.Desktop.Application.AI;

/// <summary>The Prompt Templates UI's data source - a thin read-through over <see cref="Rojan.Desktop.Domain.AI.IAIRepository"/>'s template storage.</summary>
public interface IPromptTemplateRepository
{
    public Task<IReadOnlyList<PromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    public Task<PromptTemplateDto?> GetTemplateByIdAsync(string templateId, CancellationToken cancellationToken = default);

    public Task<PromptTemplateDto?> GetTemplateForCategoryAsync(InsightCategory category, CancellationToken cancellationToken = default);
}
