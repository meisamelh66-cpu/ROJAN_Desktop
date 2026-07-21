namespace Rojan.Desktop.Application.Automation;

/// <summary>One outbound email - Requirement 32.6 (Automation Notifications).</summary>
public sealed record EmailMessage(string ToAddress, string Subject, string Body);

/// <summary>
/// Email delivery contract for the <see cref="WorkflowStepType.Email"/>
/// workflow step - Requirement 32.6. No real SMTP integration in this
/// phase; <c>Infrastructure.Automation.LocalEmailOutboxService</c>
/// persists sent messages to a local outbox for inspection/testing rather
/// than actually delivering them, the same "contract now, real
/// integration later" boundary <see cref="IAiActionExecutor"/> draws for
/// AI actions.
/// </summary>
public interface IEmailNotificationService
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
