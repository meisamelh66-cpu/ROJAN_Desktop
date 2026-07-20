namespace Rojan.Desktop.Application.AI;

/// <summary>What <see cref="IAIService.SendMessageAsync"/> returns - the persisted user turn, the persisted assistant reply, and the token-usage record <see cref="ITokenUsageTracker"/> recorded for it.</summary>
public sealed record SendMessageResultDto(
    ConversationMessageDto UserMessage,
    ConversationMessageDto AssistantMessage,
    TokenUsageRecordDto TokenUsage);
