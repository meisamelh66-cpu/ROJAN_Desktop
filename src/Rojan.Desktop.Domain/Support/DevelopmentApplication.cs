namespace Rojan.Desktop.Domain.Support;

/// <summary>
/// A "درخواست مشارکت در توسعه" (development-participation request)
/// submitted through the Support Center, as returned by
/// <see cref="IDevelopmentApplicationRepository"/>. Architecture only,
/// per this sprint's own "Architecture only. Backend-ready." instruction -
/// there is no review/approval workflow yet, just durable capture of the
/// submission.
/// </summary>
public sealed record DevelopmentApplication(
    string Id,
    string FirstName,
    string LastName,
    string Mobile,
    string Email,
    string City,
    string CollaborationArea,
    string GitHubUrl,
    string LinkedInUrl,
    string PortfolioUrl,
    string ResumeUrl,
    string Description,
    DateTimeOffset SubmittedAt);
