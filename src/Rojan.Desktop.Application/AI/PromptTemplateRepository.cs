using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.AI;

public sealed class PromptTemplateRepository : IPromptTemplateRepository
{
    private readonly DomainAI.IAIRepository _repository;

    public PromptTemplateRepository(DomainAI.IAIRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _repository.GetPromptTemplatesAsync(cancellationToken).ConfigureAwait(false);
        return templates.Select(AIMapper.MapTemplate).ToList();
    }

    public async Task<PromptTemplateDto?> GetTemplateByIdAsync(string templateId, CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetPromptTemplateByIdAsync(templateId, cancellationToken).ConfigureAwait(false);
        return template is null ? null : AIMapper.MapTemplate(template);
    }

    public async Task<PromptTemplateDto?> GetTemplateForCategoryAsync(InsightCategory category, CancellationToken cancellationToken = default)
    {
        var templates = await _repository.GetPromptTemplatesAsync(cancellationToken).ConfigureAwait(false);
        var domainCategory = AIMapper.MapCategory(category);
        var match = templates.FirstOrDefault(t => t.Category == domainCategory);
        return match is null ? null : AIMapper.MapTemplate(match);
    }
}
