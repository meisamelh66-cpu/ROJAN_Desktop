using Rojan.Desktop.Application.AI;

namespace Rojan.Desktop.Application.AI.Providers;

/// <summary>One flattened turn of the request sent to an <see cref="IAIProvider"/> - the composed <see cref="PromptContextDto"/> reduced to the role/content pairs a real chat-completion API expects.</summary>
public sealed record AIProviderMessageDto(ConversationRole Role, string Content);
