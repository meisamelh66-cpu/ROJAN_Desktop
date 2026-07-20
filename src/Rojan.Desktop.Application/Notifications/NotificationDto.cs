namespace Rojan.Desktop.Application.Notifications;

/// <summary>Application-layer shape of a raised notification, mapped from <see cref="Domain.Notifications.AppNotification"/> by <see cref="NotificationService"/> - so nothing Domain-shaped crosses into Presentation, the same reasoning every other module's Dto/QueryService pair already follows.</summary>
public sealed record NotificationDto(
    string Id,
    NotificationSeverity Severity,
    NotificationPriority Priority,
    string TitleKey,
    string MessageKey,
    IReadOnlyList<string> MessageArgs,
    string Category,
    string GroupKey,
    DateTimeOffset CreatedAt,
    bool IsRead);
