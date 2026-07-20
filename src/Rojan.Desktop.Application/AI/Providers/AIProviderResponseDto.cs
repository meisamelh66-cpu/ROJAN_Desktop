namespace Rojan.Desktop.Application.AI.Providers;

public sealed record AIProviderResponseDto(string Content, int PromptTokens, int CompletionTokens);
