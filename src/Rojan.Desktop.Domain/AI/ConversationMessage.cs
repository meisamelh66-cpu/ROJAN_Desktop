namespace Rojan.Desktop.Domain.AI;

/// <summary>One turn of a <see cref="ConversationSession"/>. <see cref="TokenCount"/> is an estimate (see <c>Application.AI.TokenUsageTracker</c>), not a provider-reported exact count, since <see cref="AIProviderType.Mock"/> never talks to a real API.</summary>
public sealed record ConversationMessage(
    string Id,
    string SessionId,
    ConversationRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    int TokenCount);
