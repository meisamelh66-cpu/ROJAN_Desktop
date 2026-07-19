namespace Rojan.Desktop.Application.Services;

/// <summary>Application-layer shape of a specialist-to-service assignment, mapped from <see cref="Rojan.Desktop.Domain.Services.SpecialistService"/> by <see cref="ServiceMapper"/>.</summary>
public sealed record AssignedSpecialistDto(string Id, string ServiceId, string SpecialistId, string SpecialistName);
