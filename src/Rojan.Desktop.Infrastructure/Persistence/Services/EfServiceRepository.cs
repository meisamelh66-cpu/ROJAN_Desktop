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
/// Unlike Customers/Specialists, <see cref="DomainServices.IServiceRepository"/>
/// has no create/update-service methods at all (see its own doc comment -
/// Phase 13 scoped this module to browse-catalog-plus-specialist-
/// assignment, not catalog authoring), so this repository has no
/// corresponding <c>CreateServiceAsync</c>/<c>UpdateServiceAsync</c>
/// either - matching the interface shape exactly, nothing invented. A
/// real, known consequence: a fresh SQLite database has an empty Services
/// table, and - unlike Customers/Specialists, which grow from normal use
/// through their own create commands - there is currently no way for the
/// running app to populate it, since no command exists to create a
/// service. That gap is pre-existing (Fake*Repository's catalog was
/// always just hardcoded seed data, never truly "created" through the
/// app either) and is not something this commit's scope - matching the
/// existing repository contract exactly - can or should invent a fix for.
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
}
