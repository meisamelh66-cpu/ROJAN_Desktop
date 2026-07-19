namespace Rojan.Desktop.Application.Specialists;

/// <summary>Input to <see cref="ISpecialistCommandService.CreateSpecialistAsync"/> - new specialists always start as <c>Active</c>, so Status isn't a caller-supplied field.</summary>
public sealed record CreateSpecialistRequest(string FullName, string Title, string Email, string Phone, string Bio);
