namespace Rojan.Desktop.Application.Specialists;

/// <summary>Everything the Specialist profile screen needs for one specialist - the aggregate fetched together as a single unit of work, same reasoning as Customers.CustomerProfileDto.</summary>
public sealed record SpecialistProfileDto(SpecialistDto Specialist, IReadOnlyList<SpecialistSkillDto> Skills);
