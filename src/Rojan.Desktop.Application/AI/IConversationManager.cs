namespace Rojan.Desktop.Application.AI;

/// <summary>
/// The Conversation System's read/write surface for the ACTIVE chat
/// experience - sessions, messages, pin/unpin, search, clear, export. See
/// <see cref="IAIHistoryService"/> for read-only historical browsing
/// across every session (the History UI), a distinct concern from this
/// one's "drive the Chat Window" responsibility.
/// </summary>
public interface IConversationManager
{
    public Task<IReadOnlyList<ConversationSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default);

    public Task<ConversationSessionDto> CreateSessionAsync(string initialTitle, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ConversationMessageDto>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);

    public Task<ConversationMessageDto> AppendMessageAsync(string sessionId, ConversationRole role, string content, int tokenCount, CancellationToken cancellationToken = default);

    public Task<ConversationSessionDto> TogglePinAsync(string sessionId, CancellationToken cancellationToken = default);

    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ConversationSessionDto>> SearchSessionsAsync(string searchText, CancellationToken cancellationToken = default);

    public Task<string> ExportSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Deletes every unpinned session - pinned conversations are a deliberate exception, the same "pin protects from bulk cleanup" reasoning Saved Reports gave Phase 20's report history.</summary>
    public Task ClearHistoryAsync(CancellationToken cancellationToken = default);
}
