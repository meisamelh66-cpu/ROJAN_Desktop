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
/// </summary>
public sealed record Service(
    string Id,
    string Name,
    ServiceCategory Category,
    ServiceStatus Status,
    int DurationMinutes,
    string Price,
    string Description,
    string? CategoryName = null);
