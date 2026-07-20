namespace Rojan.Desktop.Domain.Security;

/// <summary>Phase 25: Hybrid Offline/Online Platform. Recorded when a <see cref="PendingSyncOperation"/> reaches the backend and the local and remote versions of the same entity have diverged - abstraction only (Phase 25 builds detection/recording, not a resolution UI/strategy, which a later phase owns).</summary>
public sealed record SyncConflict(
    string Id,
    string EntityType,
    string EntityId,
    string LocalVersion,
    string RemoteVersion,
    DateTimeOffset DetectedAt);
