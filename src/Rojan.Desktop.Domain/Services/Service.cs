namespace Rojan.Desktop.Domain.Services;

/// <summary>A single catalog service record, as returned by <see cref="IServiceRepository"/>.</summary>
public sealed record Service(
    string Id,
    string Name,
    ServiceCategory Category,
    ServiceStatus Status,
    int DurationMinutes,
    string Price,
    string Description);
