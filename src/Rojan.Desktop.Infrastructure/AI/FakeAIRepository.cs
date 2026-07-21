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
            new ConversationSession("session-1", "روند درآمد این ماه چطور است؟", now.AddHours(-3), now.AddHours(-3), true),
            new ConversationSession("session-2", "کدام محصولات رو به اتمام هستند؟", now.AddDays(-1), now.AddDays(-1), false),
        ];

        _messages =
        [
            new ConversationMessage("message-1", "session-1", ConversationRole.User, "روند درآمد این ماه چطور است؟", now.AddHours(-3), 8),
            new ConversationMessage("message-2", "session-1", ConversationRole.Assistant, "درآمد این ماه نسبت به ماه قبل روند صعودی دارد - برای اعداد دقیق بخش تحلیل درآمد را بررسی کنید.", now.AddHours(-3).AddSeconds(2), 24),
            new ConversationMessage("message-3", "session-2", ConversationRole.User, "کدام محصولات رو به اتمام هستند؟", now.AddDays(-1), 6),
            new ConversationMessage("message-4", "session-2", ConversationRole.Assistant, "بخش تحلیل موجودی انبار را بررسی کنید - تمام محصولاتی که به آستانه سفارش مجدد رسیده‌اند را نشان می‌دهد.", now.AddDays(-1).AddSeconds(2), 20),
        ];

        _promptTemplates =
        [
            new PromptTemplate("template-revenue", "خلاصه درآمد", InsightCategory.Revenue, "عملکرد درآمد را برای {period} خلاصه کن و روند آن را نسبت به دوره قبل نشان بده.", true),
            new PromptTemplate("template-customer", "خلاصه مشتریان", InsightCategory.Customer, "رشد و نگهداشت مشتریان را برای {period} خلاصه کن.", true),
            new PromptTemplate("template-appointment", "خلاصه نوبت‌ها", InsightCategory.Appointment, "حجم نوبت‌ها و محبوب‌ترین خدمات را برای {period} خلاصه کن.", true),
            new PromptTemplate("template-inventory", "خلاصه موجودی", InsightCategory.Inventory, "ارزش موجودی انبار و ریسک‌های کمبود کالا را برای {period} خلاصه کن.", true),
            new PromptTemplate("template-hr", "خلاصه نیروی انسانی", InsightCategory.Hr, "وضعیت نیروی انسانی و حضور و غیاب را برای {period} خلاصه کن.", true),
            new PromptTemplate("template-payroll", "خلاصه حقوق و دستمزد", InsightCategory.Payroll, "جمع حقوق و دستمزد را برای {period} خلاصه کن.", true),
            new PromptTemplate("template-attendance", "خلاصه حضور و غیاب", InsightCategory.Attendance, "نرخ حضور و هرگونه الگوی نگران‌کننده را برای {period} خلاصه کن.", true),
            new PromptTemplate("template-commission", "خلاصه کمیسیون", InsightCategory.Commission, "کمیسیون کسب‌شده هر متخصص را برای {period} خلاصه کن.", true),
            new PromptTemplate("template-general", "دستیار عمومی کسب‌وکار", InsightCategory.General, "با استفاده از اطلاعات کسب‌وکار ارائه‌شده برای {period} به سؤال کاربر پاسخ بده.", true),
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
