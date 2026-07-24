using Rojan.Desktop.Application.Security;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Sprint 7 Commit 3: Sync Producer Foundation. Wraps the real
/// <see cref="ICustomerCommandService"/> and enqueues a
/// <see cref="Domain.Security.PendingSyncOperation"/> onto
/// <see cref="ISyncQueueService"/> whenever <see cref="CreateCustomerAsync"/>/
/// <see cref="UpdateCustomerAsync"/> succeed - registered between the raw
/// <c>CustomerCommandService</c> and <see cref="CustomerCommandServicePermissionGate"/>
/// (see <c>DependencyInjection.ServiceCollectionExtensions</c>), the same
/// "wrap the public interface with a decorator" shape the permission gate
/// itself already establishes, so no caller or existing test needs to know
/// this layer exists.
///
/// Scope is deliberately narrow: only create/update (Sprint 7 Commit 3's
/// own scope) produce sync operations. Notes/tags pass straight through
/// unchanged - not because they can never matter to a future backend, but
/// because widening producer scope is a decision for a later commit, not
/// this one. Nothing is enqueued on a failed write: an exception from the
/// inner service propagates before <see cref="ISyncQueueService.EnqueueAsync"/>
/// is ever reached.
/// </summary>
public sealed class CustomerCommandServiceSyncProducer : ICustomerCommandService
{
    private readonly ICustomerCommandService _inner;
    private readonly ISyncQueueService _syncQueueService;

    public CustomerCommandServiceSyncProducer(ICustomerCommandService inner, ISyncQueueService syncQueueService)
    {
        _inner = inner;
        _syncQueueService = syncQueueService;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var created = await _inner.CreateCustomerAsync(request, cancellationToken).ConfigureAwait(true);
        await EnqueueAsync("Create", created, cancellationToken).ConfigureAwait(true);
        return created;
    }

    public async Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var updated = await _inner.UpdateCustomerAsync(request, cancellationToken).ConfigureAwait(true);
        await EnqueueAsync("Update", updated, cancellationToken).ConfigureAwait(true);
        return updated;
    }

    public Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default) =>
        _inner.AddNoteAsync(customerId, text, cancellationToken);

    public Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default) =>
        _inner.AddTagAsync(customerId, label, cancellationToken);

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default) =>
        _inner.RemoveTagAsync(customerId, tagId, cancellationToken);

    private Task EnqueueAsync(string operationType, CustomerDto dto, CancellationToken cancellationToken) =>
        _syncQueueService.EnqueueAsync(SyncOperationFactory.Create("Customer", dto.Id, operationType, dto), cancellationToken);
}
