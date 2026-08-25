using Rojan.Desktop.Application.Security;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Sprint 7 Commit 3: Sync Producer Foundation. Wraps the real
/// <see cref="ISpecialistCommandService"/> and enqueues a
/// <see cref="Domain.Security.PendingSyncOperation"/> onto
/// <see cref="ISyncQueueService"/> whenever <see cref="CreateSpecialistAsync"/>/
/// <see cref="UpdateSpecialistAsync"/> succeed - same decorator shape
/// <c>Customers.CustomerCommandServiceSyncProducer</c> establishes,
/// registered between the raw <c>SpecialistCommandService</c> and
/// <see cref="SpecialistCommandServicePermissionGate"/> (see
/// <c>DependencyInjection.ServiceCollectionExtensions</c>).
///
/// Scope is deliberately narrow: only create/update (Sprint 7 Commit 3's
/// own scope) produce sync operations - skills pass straight through
/// unchanged. Nothing is enqueued on a failed write: an exception from the
/// inner service propagates before <see cref="ISyncQueueService.EnqueueAsync"/>
/// is ever reached.
/// </summary>
public sealed class SpecialistCommandServiceSyncProducer : ISpecialistCommandService
{
    private readonly ISpecialistCommandService _inner;
    private readonly ISyncQueueService _syncQueueService;

    public SpecialistCommandServiceSyncProducer(ISpecialistCommandService inner, ISyncQueueService syncQueueService)
    {
        _inner = inner;
        _syncQueueService = syncQueueService;
    }

    public async Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        var created = await _inner.CreateSpecialistAsync(request, cancellationToken).ConfigureAwait(true);
        await EnqueueAsync("Create", created, cancellationToken).ConfigureAwait(true);
        return created;
    }

    public async Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        var updated = await _inner.UpdateSpecialistAsync(request, cancellationToken).ConfigureAwait(true);
        await EnqueueAsync("Update", updated, cancellationToken).ConfigureAwait(true);
        return updated;
    }

    public Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default) =>
        _inner.AddSkillAsync(specialistId, name, cancellationToken);

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default) =>
        _inner.RemoveSkillAsync(specialistId, skillId, cancellationToken);

    /// <summary>Specialist-Service Assignment: passes straight through unchanged, same as skills above - out of this decorator's deliberately narrow (create/update only) scope.</summary>
    public Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default) =>
        _inner.AssignServiceAsync(specialistId, serviceId, cancellationToken);

    public Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default) =>
        _inner.RemoveServiceAssignmentAsync(specialistId, serviceId, cancellationToken);

    private Task EnqueueAsync(string operationType, SpecialistDto dto, CancellationToken cancellationToken) =>
        _syncQueueService.EnqueueAsync(SyncOperationFactory.Create("Specialist", dto.Id, operationType, dto), cancellationToken);
}
