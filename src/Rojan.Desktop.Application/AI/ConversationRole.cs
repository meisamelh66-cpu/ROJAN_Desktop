namespace Rojan.Desktop.Application.AI;

/// <summary>Application's own copy of the conversation-role concept, distinct from <see cref="Rojan.Desktop.Domain.AI.ConversationRole"/> - Presentation only ever sees this one.</summary>
public enum ConversationRole
{
    System,
    Developer,
    User,
    Assistant,
}
