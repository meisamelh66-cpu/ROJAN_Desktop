namespace Rojan.Desktop.Application.Notifications;

/// <summary>Application-layer mirror of <see cref="Domain.Notifications.NotificationPriority"/> - see <see cref="NotificationSeverity"/>'s own doc comment for why Application keeps its own copy rather than reusing Domain's enum.</summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical,
}
