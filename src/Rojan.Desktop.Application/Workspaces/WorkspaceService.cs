using DomainWorkspaces = Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Application.Workspaces;

/// <summary>
/// Default <see cref="IWorkspaceService"/>. Mints workspace ids
/// (<see cref="Guid.NewGuid"/>) and timestamps (<see cref="DateTimeOffset.UtcNow"/>)
/// itself for anything created here (new workspaces) - the same "caller
/// supplies intent, service mints identity/timestamp" shape
/// <c>Notifications.NotificationService</c> already established. Ids for
/// panes/leaves created interactively (splitting, opening a tab) are minted
/// by <c>Presentation.ViewModels.Workspaces.WorkspaceHostViewModel</c> via
/// <see cref="PaneTreeRules"/> and flow straight through
/// <see cref="SaveLayoutAsync"/> unchanged. Translates between Domain's and
/// Application's own mirrored types via <see cref="WorkspaceMapping"/>, the
/// same pattern <c>NotificationService</c> established for
/// <c>NotificationSeverity</c>/<c>NotificationPriority</c>.
/// </summary>
public sealed class WorkspaceService : IWorkspaceService
{
    private const string DefaultWorkspaceName = "Default";

    private readonly DomainWorkspaces.IWorkspaceRepository _repository;

    public WorkspaceService(DomainWorkspaces.IWorkspaceRepository repository)
    {
        _repository = repository;
    }

    public event EventHandler? StateChanged;

    public async Task<IReadOnlyList<WorkspaceSummaryDto>> GetWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        var workspaces = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var activeId = await _repository.GetActiveWorkspaceIdAsync(cancellationToken).ConfigureAwait(false);
        return workspaces
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(w => MapSummary(w, activeId))
            .ToList();
    }

    public async Task<IReadOnlyList<WorkspaceSummaryDto>> GetRecentWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        var recentIds = await _repository.GetRecentWorkspaceIdsAsync(cancellationToken).ConfigureAwait(false);
        var activeId = await _repository.GetActiveWorkspaceIdAsync(cancellationToken).ConfigureAwait(false);

        var summaries = new List<WorkspaceSummaryDto>();
        foreach (var id in recentIds)
        {
            var workspace = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (workspace is not null)
            {
                summaries.Add(MapSummary(workspace, activeId));
            }
        }

        return summaries;
    }

    public async Task<WorkspaceLayoutDto> EnsureInitializedAsync(string defaultPrimaryModuleId, CancellationToken cancellationToken = default)
    {
        var workspaces = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (workspaces.Count == 0)
        {
            var now = DateTimeOffset.UtcNow;
            var created = DomainWorkspaces.WorkspaceRules.CreateDefault(NewId(), DefaultWorkspaceName, defaultPrimaryModuleId, now, isDefault: true);
            await _repository.SaveAsync(created, cancellationToken).ConfigureAwait(false);
            await _repository.SetActiveWorkspaceIdAsync(created.Id, cancellationToken).ConfigureAwait(false);
            await _repository.RecordRecentWorkspaceAsync(created.Id, cancellationToken).ConfigureAwait(false);
            return WorkspaceMapping.Map(created);
        }

        var activeId = await _repository.GetActiveWorkspaceIdAsync(cancellationToken).ConfigureAwait(false);
        var active = activeId is not null ? await _repository.GetByIdAsync(activeId, cancellationToken).ConfigureAwait(false) : null;
        if (active is not null)
        {
            return WorkspaceMapping.Map(active);
        }

        var fallback = workspaces.FirstOrDefault(w => w.IsDefault) ?? workspaces[0];
        await _repository.SetActiveWorkspaceIdAsync(fallback.Id, cancellationToken).ConfigureAwait(false);
        return WorkspaceMapping.Map(fallback);
    }

    public async Task<WorkspaceLayoutDto> GetActiveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var activeId = await _repository.GetActiveWorkspaceIdAsync(cancellationToken).ConfigureAwait(false);
        var active = activeId is not null ? await _repository.GetByIdAsync(activeId, cancellationToken).ConfigureAwait(false) : null;
        return active is null
            ? throw new InvalidOperationException($"No active workspace - call {nameof(EnsureInitializedAsync)} first.")
            : WorkspaceMapping.Map(active);
    }

    public async Task<WorkspaceLayoutDto> CreateWorkspaceAsync(string name, string primaryModuleId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var workspace = DomainWorkspaces.WorkspaceRules.CreateDefault(
            NewId(), DomainWorkspaces.WorkspaceRules.NormalizeName(name, DefaultWorkspaceName), primaryModuleId, now);

        await _repository.SaveAsync(workspace, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return WorkspaceMapping.Map(workspace);
    }

    public async Task<WorkspaceLayoutDto> DuplicateWorkspaceAsync(string workspaceId, string newName, CancellationToken cancellationToken = default)
    {
        var source = await _repository.GetByIdAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace '{workspaceId}' was not found.");

        var now = DateTimeOffset.UtcNow;
        var copy = source with
        {
            Id = NewId(),
            Name = DomainWorkspaces.WorkspaceRules.NormalizeName(newName, source.Name),
            CreatedAt = now,
            UpdatedAt = now,
            IsDefault = false,
        };

        await _repository.SaveAsync(copy, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return WorkspaceMapping.Map(copy);
    }

    public async Task RenameWorkspaceAsync(string workspaceId, string newName, CancellationToken cancellationToken = default)
    {
        var workspace = await _repository.GetByIdAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace '{workspaceId}' was not found.");

        var renamed = workspace with
        {
            Name = DomainWorkspaces.WorkspaceRules.NormalizeName(newName, workspace.Name),
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.SaveAsync(renamed, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (all.Count <= 1)
        {
            throw new InvalidOperationException("Cannot delete the only remaining workspace.");
        }

        await _repository.DeleteAsync(workspaceId, cancellationToken).ConfigureAwait(false);

        var activeId = await _repository.GetActiveWorkspaceIdAsync(cancellationToken).ConfigureAwait(false);
        if (activeId == workspaceId)
        {
            var remaining = all.Where(w => w.Id != workspaceId).ToList();
            var fallback = remaining.FirstOrDefault(w => w.IsDefault) ?? remaining[0];
            await _repository.SetActiveWorkspaceIdAsync(fallback.Id, cancellationToken).ConfigureAwait(false);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<WorkspaceLayoutDto> SwitchWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var workspace = await _repository.GetByIdAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace '{workspaceId}' was not found.");

        await _repository.SetActiveWorkspaceIdAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        await _repository.RecordRecentWorkspaceAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return WorkspaceMapping.Map(workspace);
    }

    public async Task SaveLayoutAsync(WorkspaceLayoutDto layout, CancellationToken cancellationToken = default)
    {
        var domain = WorkspaceMapping.MapToDomain(layout) with { UpdatedAt = DateTimeOffset.UtcNow };
        await _repository.SaveAsync(domain, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkspaceLayoutDto> ResetWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var workspace = await _repository.GetByIdAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace '{workspaceId}' was not found.");

        var reset = workspace with
        {
            SecondaryRoot = null,
            DockedPanels = [],
            FloatingWindows = [],
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _repository.SaveAsync(reset, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return WorkspaceMapping.Map(reset);
    }

    private static string NewId() => Guid.NewGuid().ToString("N");

    private static WorkspaceSummaryDto MapSummary(DomainWorkspaces.WorkspaceLayout workspace, string? activeId) =>
        new(workspace.Id, workspace.Name, workspace.Id == activeId, workspace.IsDefault, workspace.UpdatedAt);
}
