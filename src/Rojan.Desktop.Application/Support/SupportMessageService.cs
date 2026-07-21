using DomainSupport = Rojan.Desktop.Domain.Support;

namespace Rojan.Desktop.Application.Support;

/// <summary>Default <see cref="ISupportMessageService"/>.</summary>
public sealed class SupportMessageService : ISupportMessageService
{
    private readonly DomainSupport.ISupportMessageRepository _repository;

    public SupportMessageService(DomainSupport.ISupportMessageRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SupportMessageDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return messages.OrderByDescending(message => message.SubmittedAt).Select(SupportMapping.Map).ToList();
    }

    public async Task<SupportMessageDto> SubmitAsync(SupportMessageType type, string subject, string body, string senderName, string senderEmail, CancellationToken cancellationToken = default)
    {
        var errors = DomainSupport.SupportRules.ValidateSupportMessage(subject, body, senderEmail);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Message failed validation: {string.Join(" ", errors)}");
        }

        var message = new DomainSupport.SupportMessage(
            Guid.NewGuid().ToString("N"), SupportMapping.MapType(type), subject.Trim(), body.Trim(), senderName.Trim(), senderEmail.Trim(), DateTimeOffset.UtcNow);

        await _repository.SaveAsync(message, cancellationToken).ConfigureAwait(false);
        return SupportMapping.Map(message);
    }
}
