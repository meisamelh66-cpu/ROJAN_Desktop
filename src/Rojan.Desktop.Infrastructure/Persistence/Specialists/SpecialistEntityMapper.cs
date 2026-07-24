using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Infrastructure.Persistence.Specialists;

/// <summary>Domain&lt;-&gt;persistence-entity mapping for the Specialists vertical slice - internal, only <see cref="EfSpecialistRepository"/> calls it, same convention as every other Domain&lt;-&gt;entity mapper in this codebase (<see cref="Customers.CustomerEntityMapper"/>).</summary>
internal static class SpecialistEntityMapper
{
    public static DomainSpecialists.Specialist MapToDomain(SpecialistEntity entity) => new(
        entity.Id,
        entity.FullName,
        entity.Title,
        entity.Email,
        entity.Phone,
        entity.Status,
        entity.Bio);

    public static SpecialistEntity MapToEntity(DomainSpecialists.Specialist specialist) => new()
    {
        Id = specialist.Id,
        FullName = specialist.FullName,
        Title = specialist.Title,
        Email = specialist.Email,
        Phone = specialist.Phone,
        Status = specialist.Status,
        Bio = specialist.Bio,
    };

    /// <summary>
    /// Applies every field from <paramref name="specialist"/> onto the
    /// already-tracked <paramref name="entity"/> - a full replace, matching
    /// <c>FakeSpecialistRepository.UpdateSpecialistAsync</c>'s own
    /// <c>_specialists[index] = specialist</c> semantics exactly. Status
    /// transition validation already lives in
    /// <c>Application.Specialists.SpecialistCommandService.UpdateSpecialistAsync</c>
    /// (via <c>Domain.Specialists.SpecialistRules</c>) - this repository
    /// never re-validates it, same "dumb full replace" contract
    /// <c>Customers.CustomerEntityMapper.ApplyTo</c> already establishes.
    /// </summary>
    public static void ApplyTo(SpecialistEntity entity, DomainSpecialists.Specialist specialist)
    {
        entity.FullName = specialist.FullName;
        entity.Title = specialist.Title;
        entity.Email = specialist.Email;
        entity.Phone = specialist.Phone;
        entity.Status = specialist.Status;
        entity.Bio = specialist.Bio;
    }

    public static DomainSpecialists.SpecialistSkill MapToDomain(SpecialistSkillEntity entity) =>
        new(entity.Id, entity.SpecialistId, entity.Name);

    public static SpecialistSkillEntity MapToEntity(DomainSpecialists.SpecialistSkill skill) => new()
    {
        Id = skill.Id,
        SpecialistId = skill.SpecialistId,
        Name = skill.Name,
    };
}
