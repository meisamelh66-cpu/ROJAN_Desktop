using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Specialists;

namespace Rojan.Server.Infrastructure.Persistence.Repositories;

/// <summary>
/// Default <see cref="ISpecialistRepository"/> - same "inject the scoped
/// DbContext directly" reasoning as <c>Repositories.EfCustomerRepository</c>'s
/// own doc comment. <see cref="GetByIdAsync"/>/<see cref="GetByOrganizationIdAsync"/>
/// both filter on <c>OrganizationId</c> directly in the query - the actual
/// enforcement point for "a query from one organization must never return
/// another organization's data" (the index on that column, added by
/// <c>Configurations.SpecialistConfiguration</c>, only keeps that filter
/// cheap; it is not what makes it correct).
/// </summary>
public sealed class EfSpecialistRepository : ISpecialistRepository
{
    private readonly RojanServerDbContext _dbContext;

    public EfSpecialistRepository(RojanServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Specialist> CreateAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        _dbContext.Specialists.Add(specialist);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return specialist;
    }

    public Task<Specialist?> GetByIdAsync(string organizationId, string specialistId, CancellationToken cancellationToken = default) =>
        _dbContext.Specialists.FirstOrDefaultAsync(
            specialist => specialist.Id == specialistId && specialist.OrganizationId == organizationId,
            cancellationToken);

    public async Task<IReadOnlyList<Specialist>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        await _dbContext.Specialists
            .Where(specialist => specialist.OrganizationId == organizationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<Specialist> UpdateAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Specialists
            .FirstOrDefaultAsync(
                tracked => tracked.Id == specialist.Id && tracked.OrganizationId == specialist.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // The caller (Application.Specialists.SpecialistService) always
            // loads via GetByIdAsync first, which is itself tenant-scoped -
            // reaching here with no matching row means the organization id
            // was tampered with between load and save, which must never
            // silently succeed as an update to the wrong tenant's data.
            throw new InvalidOperationException($"Specialist '{specialist.Id}' was not found for organization '{specialist.OrganizationId}'.");
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(specialist);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return specialist;
    }
}
