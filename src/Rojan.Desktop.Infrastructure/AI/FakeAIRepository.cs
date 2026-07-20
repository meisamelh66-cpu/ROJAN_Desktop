using Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Infrastructure.AI;

/// <summary>
/// In-memory <see cref="IAIRepository"/>. Seeds two sample conversations
/// (one pinned), one system-defined prompt template per
/// <see cref="InsightCategory"/>, a default Mock provider configuration,
/// default AI settings, and a few token-usage records so the Usage
/// Dashboard has real content on first launch. Registered as a singleton
/// (same reasoning as every other Fake repository with real writes -
/// <c>FakeReportingRepository</c>, <c>FakeHrRepository</c>).
/// </summary>
public sealed class FakeAIRepository : IAIRepository
{
    private readonly List<ConversationSession> _sessions;
    private readonly List<ConversationMessage> _messages;
    private readonly List<PromptTemplate> _promptTemplates;
    private AIProviderConfiguration _providerConfiguration;
    private AISettings _settings;
    private readonly List<TokenUsageRecord> _tokenUsage;

    public FakeAIRepository()
    {
        var now = DateTimeOffset.Now;

        _sessions =
        [
            new ConversationSession("session-1", "How is revenue trending this month?", now.AddHours(-3), now.AddHours(-3), true),
            new ConversationSession("session-2", "Which products are running low?", now.AddDays(-1), now.AddDays(-1), false),
        ];

        _messages =
        [
            new ConversationMessage("message-1", "session-1", ConversationRole.User, "How is revenue trending this month?", now.AddHours(-3), 8),
            new ConversationMessage("message-2", "session-1", ConversationRole.Assistant, "Revenue this month is trending up compared to last month - keep an eye on the Revenue Insights section for the exact numbers.", now.AddHours(-3).AddSeconds(2), 24),
            new ConversationMessage("message-3", "session-2", ConversationRole.User, "Which products are running low?", now.AddDays(-1), 6),
            new ConversationMessage("message-4", "session-2", ConversationRole.Assistant, "Check the Inventory Insights section - it lists every product at or below its reorder threshold.", now.AddDays(-1).AddSeconds(2), 20),
        ];

        _promptTemplates =
        [
            new PromptTemplate("template-revenue", "Revenue Snapshot", InsightCategory.Revenue, "Summarize revenue performance for {period}, highlighting the trend versus the prior period.", true),
            new PromptTemplate("template-customer", "Customer Snapshot", InsightCategory.Customer, "Summarize customer growth and retention for {period}.", true),
            new PromptTemplate("template-appointment", "Appointments Snapshot", InsightCategory.Appointment, "Summarize appointment volume and the most popular services for {period}.", true),
            new PromptTemplate("template-inventory", "Inventory Snapshot", InsightCategory.Inventory, "Summarize inventory value and any low-stock risks for {period}.", true),
            new PromptTemplate("template-hr", "Staff Snapshot", InsightCategory.Hr, "Summarize staffing levels and attendance for {period}.", true),
            new PromptTemplate("template-payroll", "Payroll Snapshot", InsightCategory.Payroll, "Summarize payroll totals for {period}.", true),
            new PromptTemplate("template-attendance", "Attendance Snapshot", InsightCategory.Attendance, "Summarize attendance rate and any concerning patterns for {period}.", true),
            new PromptTemplate("template-commission", "Commission Snapshot", InsightCategory.Commission, "Summarize commission earned per specialist for {period}.", true),
            new PromptTemplate("template-general", "General Business Assistant", InsightCategory.General, "Answer the user's question using the supplied business context for {period}.", true),
        ];

        _providerConfiguration = new AIProviderConfiguration(AIProviderType.Mock, "rojan-mock-v1", true);
        _settings = new AISettings(true, true, true, true);

        _tokenUsage =
        [
            new TokenUsageRecord("usage-1", "session-1", AIProviderType.Mock, 8, 24, now.AddHours(-3)),
            new TokenUsageRecord("usage-2", "session-2", AIProviderType.Mock, 6, 20, now.AddDays(-1)),
        ];
    }

    public Task<IReadOnlyList<ConversationSession>> GetSessionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ConversationSession>>(_sessions.OrderByDescending(s => s.UpdatedAt).ToList());

    public Task<ConversationSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.FirstOrDefault(s => s.Id == sessionId));

    public Task<ConversationSession> CreateSessionAsync(ConversationSession session, CancellationToken cancellationToken = default)
    {
        _sessions.Add(session);
        return Task.FromResult(session);
    }

    public Task<ConversationSession> UpdateSessionAsync(ConversationSession session, CancellationToken cancellationToken = default)
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

    public Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ConversationMessage>>(_messages.Where(m => m.SessionId == sessionId).OrderBy(m => m.CreatedAt).ToList());

    public Task<ConversationMessage> AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<PromptTemplate>> GetPromptTemplatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PromptTemplate>>(_promptTemplates);

    public Task<PromptTemplate?> GetPromptTemplateByIdAsync(string templateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_promptTemplates.FirstOrDefault(t => t.Id == templateId));

    public Task<AIProviderConfiguration> GetProviderConfigurationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_providerConfiguration);

    public Task<AIProviderConfiguration> SetProviderConfigurationAsync(AIProviderConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _providerConfiguration = configuration;
        return Task.FromResult(_providerConfiguration);
    }

    public Task<AISettings> GetSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_settings);

    public Task<AISettings> SetSettingsAsync(AISettings settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }

    public Task<TokenUsageRecord> RecordTokenUsageAsync(TokenUsageRecord record, CancellationToken cancellationToken = default)
    {
        _tokenUsage.Add(record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<TokenUsageRecord>> GetTokenUsageAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TokenUsageRecord>>(_tokenUsage.OrderByDescending(u => u.RecordedAt).ToList());
}
