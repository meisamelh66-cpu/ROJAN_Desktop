namespace Rojan.Desktop.Application.AI;

/// <summary>The Model Selector's data source - which <see cref="AIProviderType"/> and model id is active. Never carries a credential; see <see cref="Providers.IAIProvider"/>'s own doc comment for why.</summary>
public interface IAIConfigurationService
{
    public Task<AIProviderConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);

    public Task<AIProviderConfigurationDto> SetConfigurationAsync(AIProviderType providerType, string modelId, bool isEnabled, CancellationToken cancellationToken = default);

    public IReadOnlyList<AIProviderType> GetAvailableProviderTypes();
}
