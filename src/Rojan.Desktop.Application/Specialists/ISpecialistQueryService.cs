namespace Rojan.Desktop.Application.Specialists;

/// <summary>Read-only use case Presentation depends on to load Specialists - the only way Presentation ever reaches specialist data, never through Domain/Infrastructure directly.</summary>
public interface ISpecialistQueryService
{
    public Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns specialists whose name, title, or email contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every specialist.</summary>
    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default);
}
