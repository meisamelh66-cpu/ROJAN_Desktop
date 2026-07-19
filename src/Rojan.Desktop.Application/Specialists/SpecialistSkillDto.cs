namespace Rojan.Desktop.Application.Specialists;

/// <summary>Application-layer shape of a specialist skill, mapped from <see cref="Rojan.Desktop.Domain.Specialists.SpecialistSkill"/> by <see cref="SpecialistMapper"/>.</summary>
public sealed record SpecialistSkillDto(string Id, string SpecialistId, string Name);
