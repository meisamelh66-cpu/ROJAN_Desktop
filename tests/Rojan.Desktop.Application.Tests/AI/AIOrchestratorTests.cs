using Rojan.Desktop.Application.AI;
using Rojan.Desktop.Application.AI.Providers;

namespace Rojan.Desktop.Application.Tests.AI;

internal sealed class StubPromptBuilder : IPromptBuilder
{
    public Task<PromptContextDto> BuildAsync(string userMessage, int sessionMessageCount, LanguageContextDto languageContext, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PromptContextDto("System prompt.", "Developer prompt.", userMessage, "Business context.", "Analytics context.", languageContext.LanguageName, $"Message #{sessionMessageCount}"));
}

public sealed class AIOrchestratorTests
{
    private static readonly LanguageContextDto EnglishContext = new("en", "English", false);

    private static (AIOrchestrator Orchestrator, IConversationManager ConversationManager, ITokenUsageTracker TokenUsageTracker) CreateSut()
    {
        var repository = new StubAIRepository();
        var conversationManager = new ConversationManager(repository);
        var tokenUsageTracker = new TokenUsageTracker(repository);
        var orchestrator = new AIOrchestrator(
            conversationManager,
            new StubPromptBuilder(),
            new MockAIProvider(),
            new ResponseFormatter(),
            tokenUsageTracker,
            new AIConfigurationService(repository));

        return (orchestrator, conversationManager, tokenUsageTracker);
    }

    [Fact]
    public async Task SendMessageAsync_PersistsBothTheUserAndAssistantMessages()
    {
        var (sut, conversationManager, _) = CreateSut();
        var session = await conversationManager.CreateSessionAsync("New conversation");

        var result = await sut.SendMessageAsync(session.Id, "How is revenue trending?", EnglishContext);

        var messages = await conversationManager.GetMessagesAsync(session.Id);
        Assert.Equal(2, messages.Count);
        Assert.Equal(ConversationRole.User, result.UserMessage.Role);
        Assert.Equal(ConversationRole.Assistant, result.AssistantMessage.Role);
        Assert.Equal("How is revenue trending?", result.UserMessage.Content);
        Assert.False(string.IsNullOrWhiteSpace(result.AssistantMessage.Content));
    }

    [Fact]
    public async Task SendMessageAsync_RecordsTokenUsageForTheProviderInConfiguration()
    {
        var (sut, conversationManager, tokenUsageTracker) = CreateSut();
        var session = await conversationManager.CreateSessionAsync("New conversation");

        var result = await sut.SendMessageAsync(session.Id, "How is revenue trending?", EnglishContext);

        var history = await tokenUsageTracker.GetUsageHistoryAsync();
        Assert.Single(history);
        Assert.Equal(AIProviderType.Mock, history[0].ProviderType);
        Assert.Equal(result.TokenUsage.TotalTokens, history[0].TotalTokens);
    }

    [Fact]
    public async Task StreamMessageAsync_YieldsChunksThenPersistsTheAssembledAssistantMessage()
    {
        var (sut, conversationManager, _) = CreateSut();
        var session = await conversationManager.CreateSessionAsync("New conversation");

        var builder = new System.Text.StringBuilder();
        await foreach (var chunk in sut.StreamMessageAsync(session.Id, "How is revenue trending?", EnglishContext))
        {
            builder.Append(chunk);
        }

        var messages = await conversationManager.GetMessagesAsync(session.Id);
        Assert.Equal(2, messages.Count);
        var assistantMessage = messages.Single(m => m.Role == ConversationRole.Assistant);
        Assert.Equal(builder.ToString(), assistantMessage.Content);
    }
}
