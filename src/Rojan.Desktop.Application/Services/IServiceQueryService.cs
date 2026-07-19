namespace Rojan.Desktop.Application.Services;

/// <summary>Read-only use case Presentation depends on to load the service catalog - the only way Presentation ever reaches service data, never through Domain/Infrastructure directly.</summary>
public interface IServiceQueryService
{
    public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns services whose name, category, or description contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every service.</summary>
    public Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(string searchText, CancellationToken cancellationToken = default);
}
