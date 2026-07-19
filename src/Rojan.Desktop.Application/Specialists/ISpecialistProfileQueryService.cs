namespace Rojan.Desktop.Application.Specialists;

/// <summary>Read-only use case Presentation depends on to load one specialist's profile (details plus skills) as a single unit of work.</summary>
public interface ISpecialistProfileQueryService
{
    public Task<SpecialistProfileDto> GetProfileAsync(string specialistId, CancellationToken cancellationToken = default);
}
