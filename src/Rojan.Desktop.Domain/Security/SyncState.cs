namespace Rojan.Desktop.Domain.Security;

/// <summary>Phase 25: Hybrid Offline/Online Platform. Lifecycle stage of the sync queue, as observed by <c>Infrastructure.Sync.SyncQueueService</c>.</summary>
public enum SyncState
{
    Idle,
    Syncing,
    PendingChanges,
    ConflictDetected,
    Failed,
}
