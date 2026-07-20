namespace Rojan.Desktop.Domain.AI;

/// <summary>
/// Repository abstraction for the AI Center vertical slice. Like
/// <c>Reporting.IReportingRepository</c> before it, this deliberately does
/// NOT own the business data insights/recommendations are computed from -
/// that stays in each source module's own repository, reached only
/// through their already-published Application-layer query services (see
/// <c>Application.AI.InsightEngine</c>). This repository owns exactly what
/// is genuinely local to AI Center: conversation history, prompt
/// templates, provider/model selection, token usage history, and feature
/// settings. "Dumb" like every other repository in this app - no
/// aggregation or generation logic here.
/// </summary>
public interface IAIRepository
{
    public Task<IReadOnlyList<ConversationSession>> GetSessionsAsync(CancellationToken cancellationToken = default);

    public Task<ConversationSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);

    public Task<ConversationSession> CreateSessionAsync(ConversationSession session, CancellationToken cancellationToken = default);

    public Task<ConversationSession> UpdateSessionAsync(ConversationSession session, CancellationToken cancellationToken = default);

    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);

    public Task<ConversationMessage> AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PromptTemplate>> GetPromptTemplatesAsync(CancellationToken cancellationToken = default);

    public Task<PromptTemplate?> GetPromptTemplateByIdAsync(string templateId, CancellationToken cancellationToken = default);

    public Task<AIProviderConfiguration> GetProviderConfigurationAsync(CancellationToken cancellationToken = default);

    public Task<AIProviderConfiguration> SetProviderConfigurationAsync(AIProviderConfiguration configuration, CancellationToken cancellationToken = default);

    public Task<AISettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    public Task<AISettings> SetSettingsAsync(AISettings settings, CancellationToken cancellationToken = default);

    public Task<TokenUsageRecord> RecordTokenUsageAsync(TokenUsageRecord record, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<TokenUsageRecord>> GetTokenUsageAsync(CancellationToken cancellationToken = default);
}
