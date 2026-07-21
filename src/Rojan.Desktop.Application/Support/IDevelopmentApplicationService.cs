namespace Rojan.Desktop.Application.Support;

/// <summary>"درخواست مشارکت در توسعه" (development-participation request) submission. Architecture only, per this sprint's own "Architecture only. Backend-ready." instruction - no review/approval workflow yet.</summary>
public interface IDevelopmentApplicationService
{
    public Task<IReadOnlyList<DevelopmentApplicationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates and persists a new application. Throws <see cref="InvalidOperationException"/> if required fields are missing.</summary>
    public Task<DevelopmentApplicationDto> SubmitAsync(
        string firstName,
        string lastName,
        string mobile,
        string email,
        string city,
        string collaborationArea,
        string gitHubUrl,
        string linkedInUrl,
        string portfolioUrl,
        string resumeUrl,
        string description,
        CancellationToken cancellationToken = default);
}
