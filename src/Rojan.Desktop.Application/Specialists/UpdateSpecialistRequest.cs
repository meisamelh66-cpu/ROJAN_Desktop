namespace Rojan.Desktop.Application.Specialists;

/// <summary>Input to <see cref="ISpecialistCommandService.UpdateSpecialistAsync"/> - a full replacement of the editable fields, matching <see cref="Rojan.Desktop.Domain.Specialists.Specialist"/>'s shape as an immutable record.</summary>
public sealed record UpdateSpecialistRequest(
    string Id,
    string FullName,
    string Title,
    string Email,
    string Phone,
    SpecialistStatus Status,
    string Bio);
