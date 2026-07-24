namespace Rojan.Desktop.Domain.Security;

/// <summary>
/// Phase 25: Hybrid Offline/Online Platform. Recorded when a
/// <see cref="PendingSyncOperation"/> reaches the backend and the local
/// and remote versions of the same entity have diverged - abstraction
/// only (Phase 25 builds detection/recording, not a resolution UI/
/// strategy, which a later phase owns).
///
/// Sprint 7 Commit 4: <see cref="OperationId"/> ties a conflict back to
/// the exact <see cref="PendingSyncOperation"/> that triggered it (for
/// auditability - <c>Infrastructure.Sync.SyncQueueService</c> no longer
/// drops that operation from the queue once it has conflicted, see its
/// own doc comment), <see cref="Reason"/> is a human-readable explanation
/// distinct from the raw <see cref="RemotePayload"/> body, and
/// <see cref="ResolutionStatus"/> exists so a future resolution workflow
/// has a real field to transition rather than needing to add one.
/// <see cref="LocalPayload"/>/<see cref="RemotePayload"/> rename the
/// previous <c>LocalVersion</c>/<c>RemoteVersion</c> names to match
/// <see cref="PendingSyncOperation.Payload"/>'s own terminology, and
/// <see cref="CreatedAt"/> renames <c>DetectedAt</c> to match this
/// record's own new <see cref="OperationId"/>/<see cref="Reason"/>
/// naming style; there was exactly one call site (<c>SyncQueueService</c>
/// itself) and no Presentation/Shell code binds to any of these property
/// names, so the rename carries no behavior change.
/// </summary>
public sealed record SyncConflict(
    string Id,
    string OperationId,
    string EntityType,
    string EntityId,
    string LocalPayload,
    string RemotePayload,
    string Reason,
    DateTimeOffset CreatedAt,
    SyncConflictResolutionStatus ResolutionStatus = SyncConflictResolutionStatus.Unresolved);
