using Rojan.Desktop.Application.Support;

namespace Rojan.Desktop.Presentation.Tests.Support;

internal sealed class StubRojanBrandConfiguration : IRojanBrandConfiguration
{
    public string WebsiteUrl => "rojanai.ir";

    public string PhoneNumber => "09114050112";

    public string SupportEmail => "support@rojanai.ir";

    public string ApiBaseUrl => "api.rojanai.ir";
}

internal sealed class StubSupportMessageService : ISupportMessageService
{
    private readonly List<SupportMessageDto> _messages = [];

    public string? LastSubmittedSubject { get; private set; }

    public bool ThrowsOnSubmit { get; set; }

    public Task<IReadOnlyList<SupportMessageDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SupportMessageDto>>(_messages.ToList());

    public Task<SupportMessageDto> SubmitAsync(SupportMessageType type, string subject, string body, string senderName, string senderEmail, CancellationToken cancellationToken = default)
    {
        if (ThrowsOnSubmit)
        {
            throw new InvalidOperationException("Message failed validation.");
        }

        LastSubmittedSubject = subject;
        var message = new SupportMessageDto(Guid.NewGuid().ToString("N"), type, subject, body, senderName, senderEmail, DateTimeOffset.UtcNow);
        _messages.Add(message);
        return Task.FromResult(message);
    }
}

internal sealed class StubDevelopmentApplicationService : IDevelopmentApplicationService
{
    private readonly List<DevelopmentApplicationDto> _applications = [];

    public bool ThrowsOnSubmit { get; set; }

    public Task<IReadOnlyList<DevelopmentApplicationDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DevelopmentApplicationDto>>(_applications.ToList());

    public Task<DevelopmentApplicationDto> SubmitAsync(
        string firstName, string lastName, string mobile, string email, string city, string collaborationArea,
        string gitHubUrl, string linkedInUrl, string portfolioUrl, string resumeUrl, string description,
        CancellationToken cancellationToken = default)
    {
        if (ThrowsOnSubmit)
        {
            throw new InvalidOperationException("Application failed validation.");
        }

        var application = new DevelopmentApplicationDto(
            Guid.NewGuid().ToString("N"), firstName, lastName, mobile, email, city, collaborationArea,
            gitHubUrl, linkedInUrl, portfolioUrl, resumeUrl, description, DateTimeOffset.UtcNow);
        _applications.Add(application);
        return Task.FromResult(application);
    }
}
