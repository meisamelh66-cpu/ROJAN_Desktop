namespace Rojan.Desktop.Application.Support;

/// <summary>Application's own mirror of <c>Domain.Support.SupportMessageType</c>.</summary>
public enum SupportMessageType
{
    General,
    SuperAdmin,
    BugReport,
    Suggestion,
}

/// <summary>Application's own mirror of <c>Domain.Support.SupportMessage</c>.</summary>
public sealed record SupportMessageDto(
    string Id,
    SupportMessageType Type,
    string Subject,
    string Body,
    string SenderName,
    string SenderEmail,
    DateTimeOffset SubmittedAt);

/// <summary>Application's own mirror of <c>Domain.Support.DevelopmentApplication</c>.</summary>
public sealed record DevelopmentApplicationDto(
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
