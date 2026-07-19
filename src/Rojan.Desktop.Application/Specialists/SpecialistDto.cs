namespace Rojan.Desktop.Application.Specialists;

/// <summary>Application-layer shape of a specialist record, mapped from <see cref="Rojan.Desktop.Domain.Specialists.Specialist"/> by <see cref="SpecialistMapper"/>.</summary>
public sealed record SpecialistDto(
    string Id,
    string FullName,
    string Title,
    string Email,
    string Phone,
    SpecialistStatus Status,
    string Bio);
