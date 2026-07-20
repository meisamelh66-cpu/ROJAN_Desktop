namespace Rojan.Desktop.Domain.AI;

/// <summary>Every model backend the Provider abstraction can target. Phase 21 ships the abstraction plus <see cref="Mock"/> only - no API keys, no real network calls anywhere in this app.</summary>
public enum AIProviderType
{
    Mock,
    OpenAI,
    Anthropic,
    AzureOpenAI,
    LocalModel,
}
