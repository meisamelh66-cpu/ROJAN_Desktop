namespace Rojan.Desktop.Application.AI.Providers;

public sealed record AIProviderRequestDto(string SessionId, IReadOnlyList<AIProviderMessageDto> Messages, string ModelId);
