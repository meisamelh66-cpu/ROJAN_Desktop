using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Minimal <see cref="IEmailNotificationService"/> test double recording every <see cref="SendAsync"/> call.</summary>
internal sealed class StubEmailNotificationService : IEmailNotificationService
{
    public List<EmailMessage> SentMessages { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add(message);
        return Task.CompletedTask;
    }
}
