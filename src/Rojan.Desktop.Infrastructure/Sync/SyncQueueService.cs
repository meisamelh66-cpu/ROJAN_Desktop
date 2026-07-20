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
    }

    public SyncState CurrentState { get; private set; } = SyncState.Idle;

    public IReadOnlyList<SyncConflict> Conflicts => _conflicts;

    public event EventHandler<SyncState>? StateChanged;

    public void Dispose() => _processLock.Dispose();

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _queue.Clear();
        _queue.AddRange(ReadPersisted());
        SetState(_queue.Count == 0 ? SyncState.Idle : SyncState.PendingChanges);
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
                SetState(SyncState.PendingChanges);
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
                else if (outcome == OperationOutcome.RetryLimitReached)
                {
                    remaining.Add(operation);
                }

                // OperationOutcome.Succeeded / Conflict both drop the
                // operation from the queue - a conflict is recorded, not
                // retried indefinitely (see this class's own doc comment).
            }

            _queue.Clear();
            _queue.AddRange(remaining);
            Persist();

            SetState(_queue.Count == 0
                ? SyncState.Idle
                : _queue.Any(o => o.RetryCount < MaxRetryCount) ? SyncState.PendingChanges : SyncState.Failed);
        }
        finally
        {
            _processLock.Release();
        }
    }

    private async Task<OperationOutcome> TryProcessAsync(PendingSyncOperation operation, CancellationToken cancellationToken)
    {
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
                _conflicts.Add(new SyncConflict(
                    Guid.NewGuid().ToString("N"),
                    operation.EntityType,
                    operation.EntityId,
                    operation.Payload,
                    response.ErrorMessage ?? string.Empty,
                    DateTimeOffset.UtcNow));
                SetState(SyncState.ConflictDetected);
                return OperationOutcome.Conflict;
            }

            return OperationOutcome.Retry;
        }
        catch (ApiException)
        {
            return OperationOutcome.Retry;
        }
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

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "sync", "queue.json");

    private enum OperationOutcome
    {
        Succeeded,
        Retry,
        RetryLimitReached,
        Conflict,
    }
}
