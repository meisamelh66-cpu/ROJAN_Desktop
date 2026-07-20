namespace Rojan.Desktop.Domain.AI;

/// <summary>Who authored a <see cref="ConversationMessage"/> - mirrors the four prompt roles the Prompt System composes (System/Developer/User) plus the model's own reply.</summary>
public enum ConversationRole
{
    System,
    Developer,
    User,
    Assistant,
}
