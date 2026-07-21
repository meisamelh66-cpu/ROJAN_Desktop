using DomainSupport = Rojan.Desktop.Domain.Support;

namespace Rojan.Desktop.Application.Support;

/// <summary>Centralized Domain&lt;-&gt;DTO translation for the Support vertical slice - the same "one mapping class per slice" pattern every other phase in this app already establishes.</summary>
internal static class SupportMapping
{
    public static SupportMessageDto Map(DomainSupport.SupportMessage message) => new(
        message.Id, MapType(message.Type), message.Subject, message.Body, message.SenderName, message.SenderEmail, message.SubmittedAt);

    public static DomainSupport.SupportMessage MapToDomain(SupportMessageDto dto) => new(
        dto.Id, MapType(dto.Type), dto.Subject, dto.Body, dto.SenderName, dto.SenderEmail, dto.SubmittedAt);

    public static SupportMessageType MapType(DomainSupport.SupportMessageType type) => type switch
    {
        DomainSupport.SupportMessageType.General => SupportMessageType.General,
        DomainSupport.SupportMessageType.SuperAdmin => SupportMessageType.SuperAdmin,
        DomainSupport.SupportMessageType.BugReport => SupportMessageType.BugReport,
        DomainSupport.SupportMessageType.Suggestion => SupportMessageType.Suggestion,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown SupportMessageType."),
    };

    public static DomainSupport.SupportMessageType MapType(SupportMessageType type) => type switch
    {
        SupportMessageType.General => DomainSupport.SupportMessageType.General,
        SupportMessageType.SuperAdmin => DomainSupport.SupportMessageType.SuperAdmin,
        SupportMessageType.BugReport => DomainSupport.SupportMessageType.BugReport,
        SupportMessageType.Suggestion => DomainSupport.SupportMessageType.Suggestion,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown SupportMessageType."),
    };

    public static DevelopmentApplicationDto Map(DomainSupport.DevelopmentApplication application) => new(
        application.Id, application.FirstName, application.LastName, application.Mobile, application.Email, application.City,
        application.CollaborationArea, application.GitHubUrl, application.LinkedInUrl, application.PortfolioUrl,
        application.ResumeUrl, application.Description, application.SubmittedAt);

    public static DomainSupport.DevelopmentApplication MapToDomain(DevelopmentApplicationDto dto) => new(
        dto.Id, dto.FirstName, dto.LastName, dto.Mobile, dto.Email, dto.City,
        dto.CollaborationArea, dto.GitHubUrl, dto.LinkedInUrl, dto.PortfolioUrl,
        dto.ResumeUrl, dto.Description, dto.SubmittedAt);
}
