using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Application.Tests.Security;

/// <summary>
/// In-memory <see cref="ISyncQueueService"/> test double - exposes every
/// enqueued <see cref="PendingSyncOperation"/> directly so producer tests
/// (e.g. <c>Customers.CustomerCommandServiceSyncProducerTests</c>) can
/// assert on exactly what was queued without depending on
/// <c>Infrastructure.Sync.SyncQueueService</c>'s file persistence.
/// </summary>
internal sealed class FakeSyncQueueService : ISyncQueueService
{
    public List<PendingSyncOperation> Enqueued { get; } = [];

    public SyncState CurrentState { get; private set; } = SyncState.Idle;

    public IReadOnlyList<SyncConflict> Conflicts => [];

    public event EventHandler<SyncState>? StateChanged;

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task EnqueueAsync(PendingSyncOperation operation, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(operation);
        CurrentState = SyncState.PendingChanges;
        StateChanged?.Invoke(this, CurrentState);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PendingSyncOperation>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PendingSyncOperation>>(Enqueued.ToList());

    public Task ProcessQueueAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
