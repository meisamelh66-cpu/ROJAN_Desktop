namespace Rojan.Desktop.Application.AI;

public sealed record ConversationMessageDto(
    string Id,
    string SessionId,
    ConversationRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    int TokenCount);
