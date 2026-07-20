namespace Rojan.Desktop.Domain.Notifications;

/// <summary>Phase 27: Enterprise Notification Center. The four kinds of notification the spec names - drives both the Fluent icon/color chosen at display time and the severity filter in the Notification Center panel.</summary>
public enum NotificationSeverity
{
    Information,
    Success,
    Warning,
    Error,
}
