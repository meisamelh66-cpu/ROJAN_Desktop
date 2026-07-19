using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>Domain&lt;-&gt;Application mapping shared by every Specialists use case - same reasoning as <c>Customers.CustomerMapper</c>.</summary>
internal static class SpecialistMapper
{
    public static SpecialistDto MapSpecialist(DomainSpecialists.Specialist specialist) => new(
        specialist.Id,
        specialist.FullName,
        specialist.Title,
        specialist.Email,
        specialist.Phone,
        MapStatus(specialist.Status),
        specialist.Bio);

    public static SpecialistSkillDto MapSkill(DomainSpecialists.SpecialistSkill skill) =>
        new(skill.Id, skill.SpecialistId, skill.Name);

    public static SpecialistStatus MapStatus(DomainSpecialists.SpecialistStatus status) => status switch
    {
        DomainSpecialists.SpecialistStatus.Active => SpecialistStatus.Active,
        DomainSpecialists.SpecialistStatus.OnLeave => SpecialistStatus.OnLeave,
        DomainSpecialists.SpecialistStatus.Inactive => SpecialistStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain specialist status."),
    };

    public static DomainSpecialists.SpecialistStatus MapStatusToDomain(SpecialistStatus status) => status switch
    {
        SpecialistStatus.Active => DomainSpecialists.SpecialistStatus.Active,
        SpecialistStatus.OnLeave => DomainSpecialists.SpecialistStatus.OnLeave,
        SpecialistStatus.Inactive => DomainSpecialists.SpecialistStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application specialist status."),
    };
}
