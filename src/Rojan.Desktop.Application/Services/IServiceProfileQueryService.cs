namespace Rojan.Desktop.Application.Services;

/// <summary>Read-only use case Presentation depends on to load one service's profile (details plus assigned specialists) as a single unit of work.</summary>
public interface IServiceProfileQueryService
{
    public Task<ServiceProfileDto> GetProfileAsync(string serviceId, CancellationToken cancellationToken = default);
}
