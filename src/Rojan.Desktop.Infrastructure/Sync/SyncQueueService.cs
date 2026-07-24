using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Infrastructure.Sync;

/// <summary>
/// Default <see cref="ISyncQueueService"/>. Persists the queue to
/// <c>%LocalAppData%\RojanDesktop\sync\queue.json</c> so pending
/// operations survive a restart, and actually attempts to drain it
/// through <see cref="IApiClient"/> (posting each
/// <see cref="PendingSyncOperation"/> to <c>sync/operations</c>) rather
/// than simulating success - since no backend is configured yet (Phase
/// 25 builds the platform, not the backend), every drain attempt today
/// genuinely fails with a connectivity/not-configured error and the
/// operation stays queued with its retry count incremented, which is the
/// honest behavior until a real endpoint exists. A response with
/// <see cref="ApiResponse{T}.StatusCode"/> 409 is recorded as a
/// <see cref="SyncConflict"/> rather than retried - Phase 25 only builds
/// the detection/recording seam, not a resolution strategy.
///
/// Sprint 7 Commit 4: conflicts are now persisted too, the same way the
/// queue itself already is - a sibling
/// <c>%LocalAppData%\RojanDesktop\sync\conflicts.json</c> file, so a
/// conflict recorded before a restart is not silently lost (there was no
/// storage for it at all before this commit). The operation that
/// triggered a conflict is also no longer dropped from the queue - it
/// stays (see <see cref="TryProcessAsync"/>/<see cref="ProcessQueueAsync"/>),
/// both for auditability and so <see cref="GetPendingAsync"/> stays the
/// single source of truth for "what does this device still know about,"
/// conflicted or not. <see cref="HasRecordedConflict"/> prevents a
/// conflicted operation from ever being resent (which would otherwise
/// create a duplicate <see cref="SyncConflict"/> every single
/// <see cref="ProcessQueueAsync"/> pass) or counting toward
/// <see cref="ComputeState"/>'s notion of "still pending."
/// </summary>
public sealed class SyncQueueService : ISyncQueueService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>An operation that has failed this many times is treated as permanently failed rather than retried forever - it stays queued (so it is still visible/recoverable) but no longer attempted automatically.</summary>
    public const int MaxRetryCount = 10;

    private readonly IConnectivityService _connectivityService;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IApiClient _apiClient;
    private readonly string _filePath;
    private readonly string _conflictsFilePath;
    private readonly List<PendingSyncOperation> _queue = [];
    private readonly List<SyncConflict> _conflicts = [];
    private readonly SemaphoreSlim _processLock = new(1, 1);

    public SyncQueueService(IConnectivityService connectivityService, IRetryPolicy retryPolicy, IApiClient apiClient)
        : this(connectivityService, retryPolicy, apiClient, DefaultFilePath())
    {
    }

    internal SyncQueueService(IConnectivityService connectivityService, IRetryPolicy retryPolicy, IApiClient apiClient, string filePath)
    {
        _connectivityService = connectivityService;
        _retryPolicy = retryPolicy;
        _apiClient = apiClient;
        _filePath = filePath;
        _conflictsFilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, "conflicts.json");
    }

    public SyncState CurrentState { get; private set; } = SyncState.Idle;

    public IReadOnlyList<SyncConflict> Conflicts => _conflicts;

    public event EventHandler<SyncState>? StateChanged;

    public void Dispose() => _processLock.Dispose();

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _queue.Clear();
        _queue.AddRange(ReadPersisted());
        _conflicts.Clear();
        _conflicts.AddRange(ReadPersistedConflicts());
        SetState(ComputeState());
        return Task.CompletedTask;
    }

    public Task EnqueueAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default)
    {
        _queue.Add(operation);
        Persist();
        SetState(SyncState.PendingChanges);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PendingSyncOperation>>(_queue.ToList());

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        if (!await _processLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            // Already draining - a concurrent caller does not need its
            // own pass, the in-flight one will reflect the same queue.
            return;
        }

        try
        {
            if (_queue.Count == 0)
            {
                SetState(SyncState.Idle);
                return;
            }

            if (_connectivityService.CurrentState != ConnectionState.Online)
            {
                SetState(ComputeState());
                return;
            }

            SetState(SyncState.Syncing);

            var remaining = new List<PendingSyncOperation>();
            foreach (var operation in _queue.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outcome = await TryProcessAsync(operation, cancellationToken).ConfigureAwait(false);
                if (outcome == OperationOutcome.Retry)
                {
                    remaining.Add(operation with { RetryCount = operation.RetryCount + 1 });
                }
                else if (outcome is OperationOutcome.RetryLimitReached or OperationOutcome.Conflict or OperationOutcome.AlreadyConflicted)
                {
                    // RetryLimitReached/AlreadyConflicted: unchanged, never
                    // retried again automatically. Conflict: just recorded
                    // this pass (see TryProcessAsync/RecordConflict) - kept
                    // for auditability rather than dropped, same as an
                    // already-known conflict.
                    remaining.Add(operation);
                }

                // Only OperationOutcome.Succeeded drops the operation from the queue.
            }

            _queue.Clear();
            _queue.AddRange(remaining);
            Persist();

            SetState(ComputeState());
        }
        finally
        {
            _processLock.Release();
        }
    }

    private async Task<OperationOutcome> TryProcessAsync(PendingSyncOperation operation, CancellationToken cancellationToken)
    {
        if (HasRecordedConflict(operation.Id))
        {
            return OperationOutcome.AlreadyConflicted;
        }

        if (operation.RetryCount >= MaxRetryCount)
        {
            return OperationOutcome.RetryLimitReached;
        }

        try
        {
            var response = await _retryPolicy
                .ExecuteAsync(ct => _apiClient.PostAsync<PendingSyncOperation, object>("sync/operations", operation, ct), cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccess)
            {
                return OperationOutcome.Succeeded;
            }

            if (response.StatusCode == 409)
            {
                RecordConflict(operation, response.ErrorMessage);
                return OperationOutcome.Conflict;
            }

            return OperationOutcome.Retry;
        }
        catch (ApiException)
        {
            return OperationOutcome.Retry;
        }
    }

    private bool HasRecordedConflict(string operationId) => _conflicts.Any(conflict => conflict.OperationId == operationId);

    private void RecordConflict(PendingSyncOperation operation, string? remotePayload)
    {
        var conflict = new SyncConflict(
            Guid.NewGuid().ToString("N"),
            operation.Id,
            operation.EntityType,
            operation.EntityId,
            operation.Payload,
            remotePayload ?? string.Empty,
            "Backend responded with HTTP 409 Conflict - the local and remote versions of this entity have diverged.",
            DateTimeOffset.UtcNow);

        _conflicts.Add(conflict);
        PersistConflicts();
        SetState(SyncState.ConflictDetected);
    }

    /// <summary>
    /// Sprint 7 Commit 4: the queue's overall <see cref="SyncState"/>,
    /// derived from <see cref="_queue"/>/<see cref="_conflicts"/> rather
    /// than just a raw count - an operation that has already conflicted
    /// (see <see cref="HasRecordedConflict"/>) is never "still pending" in
    /// the retry sense, so a queue made up entirely of conflicted
    /// operations reports <see cref="SyncState.ConflictDetected"/>, not
    /// <see cref="SyncState.Idle"/> (nothing left to do would be wrong -
    /// something needs manual attention) or
    /// <see cref="SyncState.PendingChanges"/> (it will never auto-retry).
    /// Used by both <see cref="InitializeAsync"/> (a conflict persisted
    /// before a restart must still be reflected once reloaded) and the end
    /// of <see cref="ProcessQueueAsync"/>.
    /// </summary>
    private SyncState ComputeState()
    {
        if (_queue.Count == 0)
        {
            return SyncState.Idle;
        }

        var retryable = _queue.Where(operation => !HasRecordedConflict(operation.Id)).ToList();
        if (retryable.Count == 0)
        {
            return SyncState.ConflictDetected;
        }

        return retryable.Any(operation => operation.RetryCount < MaxRetryCount) ? SyncState.PendingChanges : SyncState.Failed;
    }

    private void SetState(SyncState state)
    {
        if (CurrentState == state)
        {
            return;
        }

        CurrentState = state;
        StateChanged?.Invoke(this, state);
    }

    private List<PendingSyncOperation> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<PendingSyncOperation>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private void Persist()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(_queue, SerializerOptions));
    }

    private List<SyncConflict> ReadPersistedConflicts()
    {
        if (!File.Exists(_conflictsFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_conflictsFilePath);
            return JsonSerializer.Deserialize<List<SyncConflict>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private void PersistConflicts()
    {
        var directory = Path.GetDirectoryName(_conflictsFilePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_conflictsFilePath, JsonSerializer.Serialize(_conflicts, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "sync", "queue.json");

    private enum OperationOutcome
    {
        Succeeded,
        Retry,
        RetryLimitReached,
        Conflict,
        AlreadyConflicted,
    }
}
