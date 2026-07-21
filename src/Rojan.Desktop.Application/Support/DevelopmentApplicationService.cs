using DomainSupport = Rojan.Desktop.Domain.Support;

namespace Rojan.Desktop.Application.Support;

/// <summary>Default <see cref="IDevelopmentApplicationService"/>.</summary>
public sealed class DevelopmentApplicationService : IDevelopmentApplicationService
{
    private readonly DomainSupport.IDevelopmentApplicationRepository _repository;

    public DevelopmentApplicationService(DomainSupport.IDevelopmentApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<DevelopmentApplicationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return applications.OrderByDescending(application => application.SubmittedAt).Select(SupportMapping.Map).ToList();
    }

    public async Task<DevelopmentApplicationDto> SubmitAsync(
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
        CancellationToken cancellationToken = default)
    {
        var errors = DomainSupport.SupportRules.ValidateDevelopmentApplication(firstName, lastName, mobile, email, collaborationArea);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Application failed validation: {string.Join(" ", errors)}");
        }

        var application = new DomainSupport.DevelopmentApplication(
            Guid.NewGuid().ToString("N"), firstName.Trim(), lastName.Trim(), mobile.Trim(), email.Trim(), city.Trim(),
            collaborationArea.Trim(), gitHubUrl.Trim(), linkedInUrl.Trim(), portfolioUrl.Trim(), resumeUrl.Trim(),
            description.Trim(), DateTimeOffset.UtcNow);

        await _repository.SaveAsync(application, cancellationToken).ConfigureAwait(false);
        return SupportMapping.Map(application);
    }
}
