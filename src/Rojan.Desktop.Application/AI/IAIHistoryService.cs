namespace Rojan.Desktop.Application.AI;

/// <summary>
/// Read-only historical views over every conversation - the History UI's
/// data source. Composes <see cref="IConversationManager"/> rather than
/// duplicating its storage access, the same "compose the manager, don't
/// re-implement it" shape <c>Reporting.ReportSnapshotQueryService</c>
/// used over its own repository.
/// </summary>
public interface IAIHistoryService
{
    public Task<IReadOnlyList<ConversationSessionDto>> GetRecentConversationsAsync(int count = 10, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ConversationSessionDto>> GetPinnedConversationsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ConversationSessionDto>> SearchAsync(string searchText, CancellationToken cancellationToken = default);
}
