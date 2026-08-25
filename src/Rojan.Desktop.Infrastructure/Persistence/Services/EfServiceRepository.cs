using Microsoft.EntityFrameworkCore;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Infrastructure.Persistence.Services;

/// <summary>
/// Sprint 6 Commit 4: real EF Core-backed <see cref="DomainServices.IServiceRepository"/> -
/// the third Domain module moved off its <c>Fake*Repository</c> onto
/// <see cref="RojanDbContext"/>, same shape <see cref="Customers.EfCustomerRepository"/>/
/// <see cref="Specialists.EfSpecialistRepository"/> already establish
/// (short-lived <see cref="RojanDbContext"/> per call via
/// <see cref="IDbContextFactory{TContext}"/>, registered as a DI
/// singleton).
///
/// Service Catalog Management: <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/>/
/// <see cref="DeactivateServiceAsync"/>/<see cref="GetCategoriesAsync"/> now
/// exist on <see cref="DomainServices.IServiceRepository"/>, implemented
/// here against <see cref="ServiceEntity"/>'s existing columns only - no
/// schema change accompanies this (this class is unreferenced in DI,
/// <c>BackendServiceRepository</c> is always resolved, confirmed by
/// <c>PersistenceDependencyInjectionTests.AddInfrastructure_RegistersBackendServiceRepository</c>).
/// <see cref="ServiceEntity"/> has no category-id column - local/EF-backed
/// data has no such routing concept at all, same "the gap is a value
/// that's never produced, not a crash" reasoning <see cref="DomainServices.Service.CategoryName"/>
/// already established - so <see cref="GetCategoriesAsync"/> returns empty
/// here, and a create/update's <see cref="DomainServices.Service.CategoryId"/>
/// is silently ignored rather than persisted, since there is nowhere to put
/// it.
///
/// <see cref="GetAssignedSpecialistsAsync"/> returns assignments in
/// whatever order the store returns them - <c>FakeServiceRepository</c>
/// never orders them either.
/// </summary>
public sealed class EfServiceRepository : DomainServices.IServiceRepository
{
    private readonly IDbContextFactory<RojanDbContext> _contextFactory;

    public EfServiceRepository(IDbContextFactory<RojanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<DomainServices.Service>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.Services.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return entities.Select(ServiceEntityMapper.MapToDomain).ToList();
    }

    public async Task<DomainServices.Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(service => service.Id == serviceId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ServiceEntityMapper.MapToDomain(entity);
    }

    public async Task<IReadOnlyList<DomainServices.SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.SpecialistServices
            .AsNoTracking()
            .Where(assignment => assignment.ServiceId == serviceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(ServiceEntityMapper.MapToDomain).ToList();
    }

    public async Task<DomainServices.SpecialistService> AssignSpecialistAsync(DomainServices.SpecialistService assignment, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.SpecialistServices.Add(ServiceEntityMapper.MapToEntity(assignment));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return assignment;
    }

    public async Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.SpecialistServices
            .FirstOrDefaultAsync(assignment => assignment.ServiceId == serviceId && assignment.Id == assignmentId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        context.SpecialistServices.Remove(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Always empty - see this class's own doc comment for why local/EF-backed data has no category-id concept to enumerate.</summary>
    public Task<IReadOnlyList<DomainServices.ServiceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainServices.ServiceCategoryOption>>([]);

    public async Task<DomainServices.Service> CreateServiceAsync(DomainServices.Service service, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = ServiceEntityMapper.MapToEntity(service with { Id = Guid.NewGuid().ToString() });
        context.Services.Add(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ServiceEntityMapper.MapToDomain(entity);
    }

    public async Task<DomainServices.Service> UpdateServiceAsync(DomainServices.Service service, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Services
            .FirstOrDefaultAsync(existing => existing.Id == service.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Service '{service.Id}' was not found.");

        entity.Name = service.Name;
        entity.DurationMinutes = service.DurationMinutes;
        entity.Price = service.Price;
        entity.Description = service.Description;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ServiceEntityMapper.MapToDomain(entity);
    }

    public async Task DeactivateServiceAsync(string categoryId, string serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Services
            .FirstOrDefaultAsync(existing => existing.Id == serviceId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        entity.Status = DomainServices.ServiceStatus.Discontinued;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
