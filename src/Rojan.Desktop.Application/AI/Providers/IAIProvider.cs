using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.AI.Providers;

/// <summary>
/// The Provider abstraction every model backend implements - the thing
/// <see cref="AIOrchestrator"/> actually calls once the Prompt System has
/// composed a request. Phase 21 ships this abstraction plus
/// <see cref="MockAIProvider"/> only; OpenAI/Anthropic/AzureOpenAI/
/// LocalModel providers are future work behind the same interface, with
/// no API keys hardcoded anywhere in this app - a real implementation
/// would read its credential from OS-level secure storage, not from
/// Domain/Application code. <see cref="StreamCompleteAsync"/> exists so
/// the architecture is streaming-ready even though
/// <see cref="MockAIProvider"/> is the only implementation today.
/// </summary>
public interface IAIProvider
{
    public AIProviderType ProviderType { get; }

    public Task<AIProviderResponseDto> CompleteAsync(AIProviderRequestDto request, CancellationToken cancellationToken = default);

    public IAsyncEnumerable<string> StreamCompleteAsync(AIProviderRequestDto request, CancellationToken cancellationToken = default);
}
