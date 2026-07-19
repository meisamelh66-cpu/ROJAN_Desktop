namespace Rojan.Desktop.Application.Services;

/// <summary>Application-layer shape of a catalog service, mapped from <see cref="Rojan.Desktop.Domain.Services.Service"/> by <see cref="ServiceMapper"/>.</summary>
public sealed record ServiceDto(
    string Id,
    string Name,
    ServiceCategory Category,
    ServiceStatus Status,
    int DurationMinutes,
    string Price,
    string Description);
