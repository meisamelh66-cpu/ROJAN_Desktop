namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Everything the Specialist profile screen needs for one specialist - the
/// aggregate fetched together as a single unit of work, same reasoning as
/// Customers.CustomerProfileDto. <see cref="AssignedServices"/> added for
/// Specialist-Service Assignment - the services this specialist is
/// eligible to perform, per ROJAN_Backend's own real assignment data (see
/// <see cref="AssignedServiceDto"/>'s own doc comment).
/// </summary>
public sealed record SpecialistProfileDto(SpecialistDto Specialist, IReadOnlyList<SpecialistSkillDto> Skills, IReadOnlyList<AssignedServiceDto> AssignedServices);
