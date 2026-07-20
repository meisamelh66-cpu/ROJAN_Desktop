namespace Rojan.Desktop.Domain.Security;

/// <summary>
/// Phase 25: Hybrid Offline/Online Platform. One queued change waiting to
/// reach the (future) backend - the unit <c>Infrastructure.Sync.SyncQueueService</c>
/// persists, retries, and eventually either confirms or turns into a
/// <see cref="SyncConflict"/>. <see cref="Payload"/> is an opaque
/// serialized snapshot (this bounded context does not need to know the
/// shape of every entity type that might ever be queued) rather than a
/// generic type parameter, so the queue itself stays entity-agnostic.
/// </summary>
public sealed record PendingSyncOperation(
    string Id,
    string EntityType,
    string EntityId,
    string OperationType,
    string Payload,
    DateTimeOffset QueuedAt,
    int RetryCount = 0);
