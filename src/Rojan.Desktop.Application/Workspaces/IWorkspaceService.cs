namespace Rojan.Desktop.Application.Workspaces;

/// <summary>
/// Phase 29: Enterprise Workspace &amp; Window Management. The one centralized
/// entry point for saving/switching/restoring named workspace arrangements
/// (secondary panes, docked panels, floating windows) - no ViewModel talks
/// to <c>Domain.Workspaces.IWorkspaceRepository</c> directly.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>Fires after any mutation (create/rename/delete/switch/save/reset) - the same <c>StateChanged</c> naming convention <c>INotificationService</c> already established.</summary>
    public event EventHandler? StateChanged;

    public Task<IReadOnlyList<WorkspaceSummaryDto>> GetWorkspacesAsync(CancellationToken cancellationToken = default);

    /// <summary>The Recent Workspaces requirement's source of truth - most-recently-switched-to first.</summary>
    public Task<IReadOnlyList<WorkspaceSummaryDto>> GetRecentWorkspacesAsync(CancellationToken cancellationToken = default);

    /// <summary>First-ever-launch bootstrap: if no workspace exists yet, creates and activates a "Default" one showing <paramref name="defaultPrimaryModuleId"/>. Otherwise returns the already-active workspace (the "Restore last workspace on startup" requirement), falling back to the default workspace if the previously active one's id is missing/stale.</summary>
    public Task<WorkspaceLayoutDto> EnsureInitializedAsync(string defaultPrimaryModuleId, CancellationToken cancellationToken = default);

    public Task<WorkspaceLayoutDto> GetActiveWorkspaceAsync(CancellationToken cancellationToken = default);

    public Task<WorkspaceLayoutDto> CreateWorkspaceAsync(string name, string primaryModuleId, CancellationToken cancellationToken = default);

    public Task<WorkspaceLayoutDto> DuplicateWorkspaceAsync(string workspaceId, string newName, CancellationToken cancellationToken = default);

    public Task RenameWorkspaceAsync(string workspaceId, string newName, CancellationToken cancellationToken = default);

    /// <summary>Deletes a workspace, reassigning the active workspace if the deleted one was active. Throws <see cref="InvalidOperationException"/> if it's the only remaining workspace - there must always be at least one.</summary>
    public Task DeleteWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Activates <paramref name="workspaceId"/> and records it as the most recent - the switcher's "open" action.</summary>
    public Task<WorkspaceLayoutDto> SwitchWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Persists the given layout's full arrangement (panes/tabs/docked panels/floating windows) under its own id - the "Workspace layout persistence" / "Workspace save/restore" requirement's write path, called after every structural change the user makes.</summary>
    public Task SaveLayoutAsync(WorkspaceLayoutDto layout, CancellationToken cancellationToken = default);

    /// <summary>Clears every secondary pane, docked panel, and floating window from a workspace, leaving only its primary module - the "Reset workspace" requirement.</summary>
    public Task<WorkspaceLayoutDto> ResetWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);
}
