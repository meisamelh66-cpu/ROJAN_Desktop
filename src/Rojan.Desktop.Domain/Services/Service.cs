namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// A single catalog service record, as returned by <see cref="IServiceRepository"/>.
///
/// Owner App Service Integration: <see cref="CategoryName"/> is the
/// backend's real, owner-named category text - trailing, optional, default
/// <see langword="null"/> so every existing positional call site keeps
/// compiling unchanged. Always null for local/EF-backed data (which has no
/// such concept, only the fixed <see cref="Category"/> enum); always
/// present for backend-sourced data, where it is authoritative even when
/// <see cref="Category"/> itself had to fall back to <see cref="ServiceCategory.Other"/>.
///
/// Service Catalog Management: <see cref="CategoryId"/> is the backend's real
/// category id (a service only resolves through its owning category there -
/// see <see cref="IServiceRepository.GetServicesAsync"/>'s own doc comment) -
/// trailing, optional, default empty so every existing positional call site
/// keeps compiling unchanged, same pattern as <see cref="CategoryName"/>.
/// Required to address <c>/categories/{categoryId}/services/{serviceId}</c>
/// for update/deactivate; empty for local/EF-backed data, which has no such
/// routing concept.
/// </summary>
public sealed record Service(
    string Id,
    string Name,
    ServiceCategory Category,
    ServiceStatus Status,
    int DurationMinutes,
    string Price,
    string Description,
    string? CategoryName = null,
    string CategoryId = "");
