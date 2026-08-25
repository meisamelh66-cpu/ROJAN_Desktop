namespace Rojan.Desktop.Application.Services;

/// <summary>Input to <see cref="IServiceCommandService.CreateServiceAsync"/> - new services always start as <c>Active</c>, so Status isn't a caller-supplied field, matching <c>Specialists.CreateSpecialistRequest</c>'s own reasoning. <see cref="CategoryId"/> selects a real category from <see cref="IServiceQueryService.GetCategoriesAsync"/> - a service only resolves through its owning category on ROJAN_Backend.</summary>
public sealed record CreateServiceRequest(string Name, string CategoryId, int DurationMinutes, decimal Price, string Description);
