using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence.Services;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Infrastructure.Persistence.Specialists;

/// <summary>
/// Sprint 6 Commit 3: real EF Core-backed <see cref="DomainSpecialists.ISpecialistRepository"/> -
/// the second Domain module moved off its <c>Fake*Repository</c> onto
/// <see cref="RojanDbContext"/>, same shape <see cref="Customers.EfCustomerRepository"/>
/// established for Customers in Commit 2 (short-lived
/// <see cref="RojanDbContext"/> per call via <see cref="IDbContextFactory{TContext}"/>,
/// registered as a DI singleton - see that class's own doc comment for
/// why).
///
/// Behavior is a deliberate, field-for-field mirror of
/// <c>FakeSpecialistRepository</c>: <see cref="GetSkillsAsync"/> returns
/// skills in whatever order the store returns them (the fake never orders
/// them either - <see cref="DomainSpecialists.SpecialistSkill"/> has no
/// timestamp to order by at all), same "full replace, no field preserved"
/// <see cref="UpdateSpecialistAsync"/> semantics, same "throw if not
/// found" on an update to a missing id. Unlike
/// <see cref="Customers.EfCustomerRepository"/>, there is no
/// Organization/Branch scoping to preserve here at all -
/// <see cref="DomainSpecialists.Specialist"/> itself has no such fields
/// (Specialists is not an Organization/Branch-scoped module, confirmed by
/// reading <c>Application.Specialists.SpecialistQueryService</c>/
/// <c>SpecialistCommandService</c>, neither of which reference
/// <c>IEnterpriseContext</c> at all).
/// </summary>
public sealed class EfSpecialistRepository : DomainSpecialists.ISpecialistRepository
{
    private readonly IDbContextFactory<RojanDbContext> _contextFactory;

    public EfSpecialistRepository(IDbContextFactory<RojanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<DomainSpecialists.Specialist>> GetSpecialistsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.Specialists.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return entities.Select(SpecialistEntityMapper.MapToDomain).ToList();
    }

    public async Task<DomainSpecialists.Specialist?> GetSpecialistByIdAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Specialists
            .AsNoTracking()
            .FirstOrDefaultAsync(specialist => specialist.Id == specialistId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : SpecialistEntityMapper.MapToDomain(entity);
    }

    public async Task<IReadOnlyList<DomainSpecialists.SpecialistSkill>> GetSkillsAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.SpecialistSkills
            .AsNoTracking()
            .Where(skill => skill.SpecialistId == specialistId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(SpecialistEntityMapper.MapToDomain).ToList();
    }

    public async Task<DomainSpecialists.Specialist> CreateSpecialistAsync(DomainSpecialists.Specialist specialist, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.Specialists.Add(SpecialistEntityMapper.MapToEntity(specialist));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return specialist;
    }

    public async Task<DomainSpecialists.Specialist> UpdateSpecialistAsync(DomainSpecialists.Specialist specialist, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.Specialists
            .FirstOrDefaultAsync(existing => existing.Id == specialist.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Specialist '{specialist.Id}' was not found.");

        SpecialistEntityMapper.ApplyTo(entity, specialist);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return specialist;
    }

    public async Task<DomainSpecialists.SpecialistSkill> AddSkillAsync(DomainSpecialists.SpecialistSkill skill, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.SpecialistSkills.Add(SpecialistEntityMapper.MapToEntity(skill));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return skill;
    }

    public async Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.SpecialistSkills
            .FirstOrDefaultAsync(skill => skill.SpecialistId == specialistId && skill.Id == skillId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        context.SpecialistSkills.Remove(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Specialist-Service Assignment: reuses the existing
    /// <see cref="SpecialistServiceEntity"/>/<c>SpecialistServices</c>
    /// table (Sprint 6 Commit 1's own migration, no new schema) purely as
    /// a <c>(SpecialistId, ServiceId)</c> join - <see cref="SpecialistServiceEntity.Id"/>
    /// and <see cref="SpecialistServiceEntity.SpecialistName"/> are that
    /// table's own legacy, service-centric fields (see its own doc
    /// comment) that this specialist-centric read/write simply does not
    /// need; a synthetic id is still generated to satisfy the table's
    /// existing primary key, and <c>SpecialistName</c> is left empty
    /// (allowed - <c>IsRequired()</c> only forbids null, not empty) since
    /// nothing in this interface's shape ever reads it back. This
    /// implementation is dormant - <c>BackendSpecialistRepository</c> is
    /// the registered <see cref="DomainSpecialists.ISpecialistRepository"/> -
    /// added only so this class keeps compiling as a faithful, honest
    /// implementer of the full interface, same as every other member
    /// above.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.SpecialistServices
            .AsNoTracking()
            .Where(assignment => assignment.SpecialistId == specialistId)
            .Select(assignment => assignment.ServiceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var alreadyAssigned = await context.SpecialistServices
            .AnyAsync(assignment => assignment.SpecialistId == specialistId && assignment.ServiceId == serviceId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyAssigned)
        {
            return;
        }

        context.SpecialistServices.Add(new SpecialistServiceEntity
        {
            Id = Guid.NewGuid().ToString(),
            SpecialistId = specialistId,
            ServiceId = serviceId,
            SpecialistName = string.Empty,
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await context.SpecialistServices
            .Where(assignment => assignment.SpecialistId == specialistId && assignment.ServiceId == serviceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entities.Count == 0)
        {
            return;
        }

        context.SpecialistServices.RemoveRange(entities);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
