namespace Rojan.Desktop.Domain.AI;

/// <summary>
/// The active model selection - which <see cref="AIProviderType"/> and
/// model id the AI Center currently targets. Deliberately carries no
/// credential/API-key field: this phase ships the provider abstraction
/// and a Mock implementation only, per its explicit "do not hardcode API
/// keys" instruction - a future phase wiring a real provider would read
/// its key from OS-level secure storage, not from this record.
/// </summary>
public sealed record AIProviderConfiguration(AIProviderType ProviderType, string ModelId, bool IsEnabled);
