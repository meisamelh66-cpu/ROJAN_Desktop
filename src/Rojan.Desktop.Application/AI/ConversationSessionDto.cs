namespace Rojan.Desktop.Application.AI;

public sealed record ConversationSessionDto(
    string Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsPinned);
