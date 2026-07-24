namespace Rojan.Desktop.Infrastructure.Persistence.Specialists;

/// <summary>EF Core persistence model for a specialist skill - field-for-field mirror of <see cref="Domain.Specialists.SpecialistSkill"/>, see <see cref="SpecialistEntity"/>'s own doc comment for why this is a separate mutable class rather than mapping the Domain record directly.</summary>
public sealed class SpecialistSkillEntity
{
    public string Id { get; set; } = string.Empty;

    public string SpecialistId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
