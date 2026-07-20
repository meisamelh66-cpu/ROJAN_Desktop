namespace Rojan.Desktop.Application.AI;

/// <summary>Cleans up a raw <see cref="Providers.IAIProvider"/> reply before it becomes a persisted <see cref="ConversationMessageDto"/> - trims stray whitespace, collapses excessive blank lines, and caps length so a runaway completion can't blow out the UI.</summary>
public interface IResponseFormatter
{
    public string Format(string rawResponse);
}
