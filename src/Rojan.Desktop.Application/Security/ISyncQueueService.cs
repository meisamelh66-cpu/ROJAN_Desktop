using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Hybrid Offline/Online Platform. The offline-first write path:
/// every mutation that will eventually need to reach a backend is queued
/// here first (durable across restarts) and drained by
/// <see cref="ProcessQueueAsync"/> once <see cref="IConnectivityService"/>
/// reports a usable connection. No module's command service enqueues to
/// this yet (there is no backend to sync to) - this phase builds the
/// queue itself as working infrastructure, not a stub, so a future
/// module integration only has to call <see cref="EnqueueAsync"/>.
/// </summary>
public interface ISyncQueueService
{
    public SyncState CurrentState { get; }

    public IReadOnlyList<SyncConflict> Conflicts { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default);

    public Task EnqueueAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Attempts to drain the queue. A no-op (leaves <see cref="CurrentState"/> at <see cref="SyncState.PendingChanges"/>) when offline, rather than throwing - callers do not need to check connectivity themselves first.</summary>
    public Task ProcessQueueAsync(CancellationToken cancellationToken = default);

    public event EventHandler<SyncState>? StateChanged;
}
