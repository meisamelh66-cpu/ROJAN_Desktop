namespace Rojan.Desktop.Domain.Workspaces;

/// <summary>
/// Repository abstraction for saved workspace layouts. Domain defines the
/// contract; Infrastructure provides the concrete implementation (local
/// JSON persistence - see <c>Infrastructure.Workspaces.LocalWorkspaceStore</c>),
/// same pattern as every other persisted concern in this app
/// (<c>ISearchHistoryStore</c>, <c>INotificationRepository</c>).
/// </summary>
public interface IWorkspaceRepository
{
    public Task<IReadOnlyList<WorkspaceLayout>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<WorkspaceLayout?> GetByIdAsync(string workspaceId, CancellationToken cancellationToken = default);

    public Task SaveAsync(WorkspaceLayout layout, CancellationToken cancellationToken = default);

    public Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default);

    public Task<string?> GetActiveWorkspaceIdAsync(CancellationToken cancellationToken = default);

    public Task SetActiveWorkspaceIdAsync(string workspaceId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<string>> GetRecentWorkspaceIdsAsync(CancellationToken cancellationToken = default);

    public Task RecordRecentWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);
}
