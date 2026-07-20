namespace Rojan.Desktop.Application.AI;

public sealed class AIHistoryService : IAIHistoryService
{
    private readonly IConversationManager _conversationManager;

    public AIHistoryService(IConversationManager conversationManager)
    {
        _conversationManager = conversationManager;
    }

    public async Task<IReadOnlyList<ConversationSessionDto>> GetRecentConversationsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var sessions = await _conversationManager.GetSessionsAsync(cancellationToken).ConfigureAwait(false);
        return sessions.OrderByDescending(s => s.UpdatedAt).Take(count).ToList();
    }

    public async Task<IReadOnlyList<ConversationSessionDto>> GetPinnedConversationsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _conversationManager.GetSessionsAsync(cancellationToken).ConfigureAwait(false);
        return sessions.Where(s => s.IsPinned).OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public Task<IReadOnlyList<ConversationSessionDto>> SearchAsync(string searchText, CancellationToken cancellationToken = default) =>
        _conversationManager.SearchSessionsAsync(searchText, cancellationToken);
}
