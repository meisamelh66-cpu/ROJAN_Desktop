namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Input to <see cref="IServiceCommandService.UpdateServiceAsync"/> - a
/// full replacement of the editable fields plus a requested status,
/// matching <see cref="Specialists.UpdateSpecialistRequest"/>'s established
/// shape. <see cref="CategoryId"/> must be the service's own existing,
/// unchanged category id (carried forward from the loaded <see cref="ServiceDto"/>) -
/// ROJAN_Backend has no field to change a service's category at all, so
/// this is never a re-pick, only a pass-through. <see cref="Price"/> is a
/// raw <see cref="decimal"/>, same reasoning as <see cref="CreateServiceRequest"/>.
/// </summary>
public sealed record UpdateServiceRequest(
    string Id,
    string CategoryId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    ServiceStatus Status);
