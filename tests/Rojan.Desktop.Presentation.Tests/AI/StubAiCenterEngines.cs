using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Presentation.Tests.AI;

/// <summary>Fakes the AI Center's cross-module analytical engines (Insight/Recommendation/Summary/BusinessHealth/Notification, plus the Chat facade <see cref="IAIService"/>) so <c>AiCenterPageViewModelTests</c> can exercise the ViewModel without any Reporting/HR plumbing - those engines are covered directly by <c>Application.Tests.AI</c>.</summary>
internal sealed class StubAIService(IConversationManager? conversationManager = null) : IAIService
{
    public int SendMessageCallCount { get; private set; }

    public string? LastUserMessage { get; private set; }

    public Func<string, string, LanguageContextDto, SendMessageResultDto>? ResultFactory { get; set; }

    public async Task<SendMessageResultDto> SendMessageAsync(string sessionId, string userMessage, LanguageContextDto languageContext, CancellationToken cancellationToken = default)
    {
        SendMessageCallCount++;
        LastUserMessage = userMessage;

        if (ResultFactory is not null)
        {
            return ResultFactory(sessionId, userMessage, languageContext);
        }

        var now = DateTimeOffset.Now;
        if (conversationManager is not null)
        {
            var userMessageDto = await conversationManager.AppendMessageAsync(sessionId, ConversationRole.User, userMessage, 4, cancellationToken).ConfigureAwait(false);
            var assistantMessageDto = await conversationManager.AppendMessageAsync(sessionId, ConversationRole.Assistant, "Stub reply.", 4, cancellationToken).ConfigureAwait(false);
            return new SendMessageResultDto(userMessageDto, assistantMessageDto, new TokenUsageRecordDto($"usage-{SendMessageCallCount}", sessionId, AIProviderType.Mock, 4, 4, 8, now));
        }

        return new SendMessageResultDto(
            new ConversationMessageDto($"user-{SendMessageCallCount}", sessionId, ConversationRole.User, userMessage, now, 4),
            new ConversationMessageDto($"assistant-{SendMessageCallCount}", sessionId, ConversationRole.Assistant, "Stub reply.", now, 4),
            new TokenUsageRecordDto($"usage-{SendMessageCallCount}", sessionId, AIProviderType.Mock, 4, 4, 8, now));
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(string sessionId, string userMessage, LanguageContextDto languageContext, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return "Stub ";
        yield return "reply.";
    }
}

internal sealed class StubBusinessHealthService(BusinessHealthScoreDto score) : IBusinessHealthService
{
    public Task<BusinessHealthScoreDto> ComputeScoreAsync(CancellationToken cancellationToken = default) => Task.FromResult(score);
}

internal sealed class StubSummaryEngine(BusinessSummaryDto dailySummary, BusinessSummaryDto? executiveSummary = null) : ISummaryEngine
{
    public Task<BusinessSummaryDto> GetDailySummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(dailySummary);

    public Task<BusinessSummaryDto> GetExecutiveSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(executiveSummary ?? dailySummary);
}

internal sealed class StubNotificationInsightService(IReadOnlyList<SmartNotificationDto> notifications) : INotificationInsightService
{
    public Task<IReadOnlyList<SmartNotificationDto>> GetSmartNotificationsAsync(CancellationToken cancellationToken = default) => Task.FromResult(notifications);
}

internal sealed class StubInsightEngine(IReadOnlyList<AIInsightDto> insights) : IInsightEngine
{
    public Task<IReadOnlyList<AIInsightDto>> GenerateInsightsAsync(InsightCategory? filter = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(filter is null ? insights : insights.Where(i => i.Category == filter).ToList());
}

internal sealed class StubRecommendationEngine(IReadOnlyList<AIRecommendationDto> recommendations, IReadOnlyList<SuggestedTaskDto> suggestedTasks) : IRecommendationEngine
{
    public Task<IReadOnlyList<AIRecommendationDto>> GenerateRecommendationsAsync(CancellationToken cancellationToken = default) => Task.FromResult(recommendations);

    public Task<IReadOnlyList<SuggestedTaskDto>> GenerateSuggestedTasksAsync(CancellationToken cancellationToken = default) => Task.FromResult(suggestedTasks);
}
