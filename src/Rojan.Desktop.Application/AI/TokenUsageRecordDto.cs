namespace Rojan.Desktop.Application.AI;

public sealed record TokenUsageRecordDto(
    string Id,
    string SessionId,
    AIProviderType ProviderType,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    DateTimeOffset RecordedAt);
