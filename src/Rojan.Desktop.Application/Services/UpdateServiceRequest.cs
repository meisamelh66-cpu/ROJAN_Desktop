namespace Rojan.Desktop.Application.Services;

/// <summary>Input to <see cref="IServiceCommandService.UpdateServiceAsync"/> - a full replacement of the editable fields. ROJAN_Backend's update endpoint has no status/category field at all (see <c>BackendServiceRepository.UpdateServiceAsync</c>'s own doc comment), so Status/CategoryId are not editable through this request - matching the real backend contract rather than offering a control that would silently do nothing.</summary>
public sealed record UpdateServiceRequest(string Id, string Name, int DurationMinutes, decimal Price, string Description);
