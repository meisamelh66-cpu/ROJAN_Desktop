using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.AI;

public sealed class AIConfigurationService : IAIConfigurationService
{
    private readonly DomainAI.IAIRepository _repository;

    public AIConfigurationService(DomainAI.IAIRepository repository)
    {
        _repository = repository;
    }

    public async Task<AIProviderConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _repository.GetProviderConfigurationAsync(cancellationToken).ConfigureAwait(false);
        return AIMapper.MapProviderConfiguration(configuration);
    }

    public async Task<AIProviderConfigurationDto> SetConfigurationAsync(AIProviderType providerType, string modelId, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var configuration = new DomainAI.AIProviderConfiguration(AIMapper.MapProviderType(providerType), modelId, isEnabled);
        var saved = await _repository.SetProviderConfigurationAsync(configuration, cancellationToken).ConfigureAwait(false);
        return AIMapper.MapProviderConfiguration(saved);
    }

    /// <summary>Every provider type the abstraction defines - only <see cref="AIProviderType.Mock"/> has a real implementation this phase; the rest are listed so the Model Selector UI can show them as "coming soon" rather than not existing at all.</summary>
    public IReadOnlyList<AIProviderType> GetAvailableProviderTypes() =>
        [AIProviderType.Mock, AIProviderType.OpenAI, AIProviderType.Anthropic, AIProviderType.AzureOpenAI, AIProviderType.LocalModel];
}
