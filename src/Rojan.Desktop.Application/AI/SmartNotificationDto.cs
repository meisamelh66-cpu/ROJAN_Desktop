namespace Rojan.Desktop.Application.AI;

public sealed record SmartNotificationDto(
    string Id,
    InsightSeverity Severity,
    string Message,
    DateTimeOffset GeneratedAt);
