using Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>In-memory <see cref="IWorkspaceRepository"/> test double - lets <see cref="MainWindowViewModel"/>'s real <c>WorkspaceService</c>/<c>WorkspaceHostViewModel</c> run against known, empty state (no persisted workspace, same as a fresh install) rather than touching the file system.</summary>
internal sealed class StubWorkspaceRepository : IWorkspaceRepository
{
    private readonly List<WorkspaceLayout> _workspaces = [];
    private readonly List<string> _recentIds = [];
    private string? _activeId;

    public Task<IReadOnlyList<WorkspaceLayout>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkspaceLayout>>(_workspaces.ToList());

    public Task<WorkspaceLayout?> GetByIdAsync(string workspaceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_workspaces.FirstOrDefault(w => w.Id == workspaceId));

    public Task SaveAsync(WorkspaceLayout layout, CancellationToken cancellationToken = default)
    {
        var index = _workspaces.FindIndex(w => w.Id == layout.Id);
        if (index >= 0)
        {
            _workspaces[index] = layout;
        }
        else
        {
            _workspaces.Add(layout);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        _workspaces.RemoveAll(w => w.Id == workspaceId);
        return Task.CompletedTask;
    }

    public Task<string?> GetActiveWorkspaceIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_activeId);

    public Task SetActiveWorkspaceIdAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        _activeId = workspaceId;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetRecentWorkspaceIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(_recentIds.ToList());

    public Task RecordRecentWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        _recentIds.RemoveAll(id => id == workspaceId);
        _recentIds.Insert(0, workspaceId);
        return Task.CompletedTask;
    }
}
