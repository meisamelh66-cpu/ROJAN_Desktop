namespace Rojan.Desktop.Application.Services;

/// <summary>Everything the Service profile screen needs for one service - the aggregate fetched together as a single unit of work, same reasoning as Customers.CustomerProfileDto.</summary>
public sealed record ServiceProfileDto(ServiceDto Service, IReadOnlyList<AssignedSpecialistDto> AssignedSpecialists);
