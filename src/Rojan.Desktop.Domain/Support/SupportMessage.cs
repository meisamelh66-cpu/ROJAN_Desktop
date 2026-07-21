namespace Rojan.Desktop.Domain.Support;

/// <summary>
/// Which Support Center channel a <see cref="SupportMessage"/> was sent
/// through - General ("ارسال پیام"), SuperAdmin ("ارتباط با Super Admin"),
/// BugReport ("گزارش خطا"), Suggestion ("پیشنهادات و انتقادات"). One
/// message shape covers all four rather than four near-identical entities,
/// since the only real difference between them is this routing tag.
/// </summary>
public enum SupportMessageType
{
    General,
    SuperAdmin,
    BugReport,
    Suggestion,
}

/// <summary>
/// A message sent from the Support Center, as returned by
/// <see cref="ISupportMessageRepository"/>. No real outbound
/// delivery exists yet (no email server, no ticketing system) - every
/// submission is persisted locally, the same "architecture and contracts
/// only" boundary Phase 32 already established for its own outbound
/// channels (<c>Automation.IEmailNotificationService</c>).
/// </summary>
public sealed record SupportMessage(
    string Id,
    SupportMessageType Type,
    string Subject,
    string Body,
    string SenderName,
    string SenderEmail,
    DateTimeOffset SubmittedAt);
