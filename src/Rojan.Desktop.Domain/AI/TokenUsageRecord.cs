namespace Rojan.Desktop.Domain.AI;

/// <summary>One recorded exchange's token cost - the Usage Dashboard's raw data source, appended by <c>Application.AI.TokenUsageTracker</c> after every <see cref="ConversationRole.Assistant"/> reply.</summary>
public sealed record TokenUsageRecord(
    string Id,
    string SessionId,
    AIProviderType ProviderType,
    int PromptTokens,
    int CompletionTokens,
    DateTimeOffset RecordedAt)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
}
