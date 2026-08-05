namespace Rojan.Desktop.Application.Services;

/// <summary>Application-layer shape of a catalog service, mapped from <see cref="Rojan.Desktop.Domain.Services.Service"/> by <see cref="ServiceMapper"/>. <see cref="CategoryName"/> mirrors <see cref="Rojan.Desktop.Domain.Services.Service.CategoryName"/>'s own doc comment - null for local/EF-backed data, the backend's real category text otherwise.</summary>
public sealed record ServiceDto(
    string Id,
    string Name,
    ServiceCategory Category,
    ServiceStatus Status,
    int DurationMinutes,
    string Price,
    string Description,
    string? CategoryName = null);
