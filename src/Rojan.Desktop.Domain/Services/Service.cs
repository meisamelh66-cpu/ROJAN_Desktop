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
/// Service Catalog Authoring: <see cref="CategoryId"/> is the real,
/// backend-owned category id - ROJAN_Backend's own <c>ServiceResponse</c>
/// carries this on the wire (<c>Api.Contracts.ServiceResponse.CategoryId</c>)
/// and it is now propagated all the way through instead of being discarded
/// during mapping. Required for <see cref="IServiceRepository.UpdateServiceAsync"/>'s
/// URL construction - ROJAN_Backend's own <c>UpdateServiceRequest</c> has no
/// field to change a service's category, so this value is read-only from
/// this layer's own point of view: it is never re-picked on edit, only
/// carried forward unchanged. Trailing, defaulted to <see cref="string.Empty"/>
/// for the same "every existing positional call site keeps compiling"
/// reasoning as <see cref="CategoryName"/> above.
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
