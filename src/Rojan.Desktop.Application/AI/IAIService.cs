namespace Rojan.Desktop.Application.AI;

/// <summary>The AI Center's single public entry point for the Chat Window / Business Assistant - sends a user turn through the full Prompt System + Provider + Conversation System pipeline and returns the persisted result. See <see cref="AIOrchestrator"/> for the composition.</summary>
public interface IAIService
{
    public Task<SendMessageResultDto> SendMessageAsync(
        string sessionId,
        string userMessage,
        LanguageContextDto languageContext,
        CancellationToken cancellationToken = default);

    /// <summary>Same pipeline as <see cref="SendMessageAsync"/>, but yields the assistant reply word-by-word as it is produced (streaming-ready UI), then persists the completed message and records token usage once the stream finishes.</summary>
    public IAsyncEnumerable<string> StreamMessageAsync(
        string sessionId,
        string userMessage,
        LanguageContextDto languageContext,
        CancellationToken cancellationToken = default);
}
