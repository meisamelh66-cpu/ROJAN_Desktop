using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Presentation.Tests.AI;

/// <summary>A minimal <see cref="DomainAI.IAIRepository"/> for driving the real <c>ConversationManager</c>/<c>AIHistoryService</c>/<c>TokenUsageTracker</c>/<c>AIConfigurationService</c>/<c>AISettingsService</c>/<c>PromptTemplateRepository</c> under <c>AiCenterPageViewModel</c> tests - same "Presentation.Tests deliberately does not reference Infrastructure" reasoning as <c>Application.Tests.AI.StubAIRepository</c>.</summary>
internal sealed class StubAIRepository : DomainAI.IAIRepository
{
    private readonly List<DomainAI.ConversationSession> _sessions = [];
    private readonly List<DomainAI.ConversationMessage> _messages = [];
    private readonly List<DomainAI.PromptTemplate> _templates;
    private readonly List<DomainAI.TokenUsageRecord> _usage = [];
    private DomainAI.AIProviderConfiguration _configuration = new(DomainAI.AIProviderType.Mock, "mock-v1", true);
    private DomainAI.AISettings _settings = new(true, true, true, true);

    public StubAIRepository()
    {
        _templates = Enum.GetValues<DomainAI.InsightCategory>()
            .Select(category => new DomainAI.PromptTemplate($"tmpl-{category}", $"{category} template", category, $"You are analyzing {category} data. {{period}}", true))
            .ToList();
    }

    public Task<IReadOnlyList<DomainAI.ConversationSession>> GetSessionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainAI.ConversationSession>>(_sessions.ToList());

    public Task<DomainAI.ConversationSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.FirstOrDefault(s => s.Id == sessionId));

    public Task<DomainAI.ConversationSession> CreateSessionAsync(DomainAI.ConversationSession session, CancellationToken cancellationToken = default)
    {
        _sessions.Add(session);
        return Task.FromResult(session);
    }

    public Task<DomainAI.ConversationSession> UpdateSessionAsync(DomainAI.ConversationSession session, CancellationToken cancellationToken = default)
    {
        var index = _sessions.FindIndex(s => s.Id == session.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Session '{session.Id}' was not found.");
        }

        _sessions[index] = session;
        return Task.FromResult(session);
    }

    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.RemoveAll(s => s.Id == sessionId);
        _messages.RemoveAll(m => m.SessionId == sessionId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainAI.ConversationMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainAI.ConversationMessage>>(_messages.Where(m => m.SessionId == sessionId).ToList());

    public Task<DomainAI.ConversationMessage> AddMessageAsync(DomainAI.ConversationMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<DomainAI.PromptTemplate>> GetPromptTemplatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainAI.PromptTemplate>>(_templates.ToList());

    public Task<DomainAI.PromptTemplate?> GetPromptTemplateByIdAsync(string templateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_templates.FirstOrDefault(t => t.Id == templateId));

    public Task<DomainAI.AIProviderConfiguration> GetProviderConfigurationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_configuration);

    public Task<DomainAI.AIProviderConfiguration> SetProviderConfigurationAsync(DomainAI.AIProviderConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _configuration = configuration;
        return Task.FromResult(_configuration);
    }

    public Task<DomainAI.AISettings> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_settings);

    public Task<DomainAI.AISettings> SetSettingsAsync(DomainAI.AISettings settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }

    public Task<DomainAI.TokenUsageRecord> RecordTokenUsageAsync(DomainAI.TokenUsageRecord record, CancellationToken cancellationToken = default)
    {
        _usage.Add(record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<DomainAI.TokenUsageRecord>> GetTokenUsageAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainAI.TokenUsageRecord>>(_usage.ToList());
}
